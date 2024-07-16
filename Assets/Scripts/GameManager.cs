using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("[ Core ]")]
	public int score;
	//public int maxLevel;
	public bool isOver;
	public GameObject line;

	[Header("[ Object Pooling ]")]
	public GameObject donglePrefab;
    public Transform dongleGroup;
    public List<Dongle> donglePool;

	public GameObject effectPrefab;
	public Transform effectGroup;
    public List<ParticleSystem> effectPool;

    [Range(1,30)]
    public int poolSize;
    public int poolCursor;
	public Dongle lastDongle;


	[Header("[ Audio ]")]
	//���� ä�θ�
	public AudioSource bgmPlayer;
    public AudioSource[] sfxPlayer;
    public AudioClip[] sfxClip;
    public enum Sfx { LevelUp, Next, Attach, Button, Over };
    int sfxCursor;

    [Header("[ UI ]")]
    public GameObject startGroup;
    public GameObject endGroup;
    public Text subScoreText;
    public Text scoreText;
    public Text maxScoreText;

    [Header("[ Etc ]")]
    public GameObject deadLine;
    public GameObject bottom;


	void Awake()
	{
        Application.targetFrameRate = 60;

        donglePool = new List<Dongle>();
        effectPool = new List<ParticleSystem>();
        for(int i=0; i<poolSize; i++)
        {
            MakeDongle();
        }

        if (!PlayerPrefs.HasKey("MaxScore"))
        {
            PlayerPrefs.SetInt("MaxScore", 0);

		}
        maxScoreText.text = PlayerPrefs.GetInt("MaxScore").ToString();
	}

	public void GameStart()
    {
        deadLine.SetActive(true);
        bottom.SetActive(true);
        scoreText.gameObject.SetActive(true);
        maxScoreText.gameObject.SetActive(true);
        startGroup.SetActive(false);

        bgmPlayer.Play();
        SfxPlay(Sfx.Button);

        Invoke("NextDongle", 1.5f);
	}

    Dongle MakeDongle()
    {
		//����Ʈ ����
		GameObject InstantEffectObj = Instantiate(effectPrefab, effectGroup);
		InstantEffectObj.name = "Effect " + effectPool.Count;
		ParticleSystem instantEffect = InstantEffectObj.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffect);

		//���� ����
		GameObject instant = Instantiate(donglePrefab, dongleGroup);
		instant.name = "Dongle " + donglePool.Count;
		//������ ������Ʈ�� 2��°�� ���� �μ��� �ڽ� ������Ʈ�� ������ �ȴ�.
		Dongle instantDongle = instant.GetComponent<Dongle>();
        instantDongle.manager = this;
		instantDongle.effect = instantEffect;
        donglePool.Add(instantDongle);

		return instantDongle;
	}

    Dongle GetDongle()
    {
        for(int i=0; i<donglePool.Count; i++)
        {
            poolCursor = (poolCursor + 1) % donglePool.Count;
            if (!donglePool[poolCursor].gameObject.activeSelf){ //Ǯ�� ��Ȱ��ȭ �Ǿ� �ִ� ������ �ִٸ�
                return donglePool[poolCursor]; //�� ������ ��ȯ
			}
        }
        return MakeDongle();
	}

    void NextDongle()
    {
        if (isOver)
            return;

        lastDongle = GetDongle();
        lastDongle.level = Random.Range(0, 3);
        lastDongle.gameObject.SetActive(true);

        SfxPlay(Sfx.Next);
        StartCoroutine(waitNext());
	}
    
    IEnumerator waitNext()
    {
        while (lastDongle != null)
        {
            yield return null; //�� ƽ�� ��ٸ���.

		}
        yield return new WaitForSeconds(2.5f);

        NextDongle();
	}

    public void TouchDown()
    {
        if(lastDongle == null) { return; }

        lastDongle.Drag();
    }

	public void TouchUp()
	{
		if (lastDongle == null) { return; }

		lastDongle.Drop();
        lastDongle = null;
	}

    public void GameOver()
    {
        if (isOver)
            return;

        isOver = true;

        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
		//1. �׾Ƴ��� ��� ���� ��������
		Dongle[] dongles = GameObject.FindObjectsOfType<Dongle>();

		//2. �߰��� �������� �� �����ϱ� ���� ����ȿ�� ����
		for (int i = 0; i < dongles.Length; i++)
		{
            dongles[i].rigid.simulated = false;
		}

		//3. �ϳ��� �����ؼ� ����� + ���� ȯ��
		for (int i = 0; i < dongles.Length; i++)
		{
			dongles[i].Hide(Vector3.up * 100);
            yield return new WaitForSeconds(0.1f);
		}

        yield return new WaitForSeconds(1f);

        //�ְ� ���� ����
        int maxScore = Mathf.Max(score, PlayerPrefs.GetInt("MaxScore"));
        PlayerPrefs.SetInt("MaxScore", maxScore);

        //���ӿ��� UI ǥ��
        subScoreText.text = "���� : " + scoreText.text;
        endGroup.SetActive(true);

        bgmPlayer.Stop();
        SfxPlay(Sfx.Over);
	}

	public void Reset()
	{
        SfxPlay(Sfx.Button);

        StartCoroutine(ResetCoroutine());
	}

    IEnumerator ResetCoroutine()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(0);
    }

	public void SfxPlay(Sfx type)
    {
        switch (type)
        {
            case Sfx.LevelUp:
                sfxPlayer[sfxCursor].clip = sfxClip[Random.Range(0, 3)];
                break;
			case Sfx.Next:
				sfxPlayer[sfxCursor].clip = sfxClip[3];
				break;
			case Sfx.Attach:
				sfxPlayer[sfxCursor].clip = sfxClip[4];
				break;
			case Sfx.Button:
				sfxPlayer[sfxCursor].clip = sfxClip[5];
				break;
			case Sfx.Over:
				sfxPlayer[sfxCursor].clip = sfxClip[6];
				break;
		}

        //�ٸ� ���尡 ����� �� �÷��̾��� Ŭ���� �ٲ�� ������ ���尡 ������ �� �ֱ⿡
        //�ٸ� ���� �÷��̾�� �Ź� �̵����� ���尡 �ߴܵ��� �ʵ��� �Ѵ�.
        sfxPlayer[sfxCursor].Play();
        //Ŀ���� ������Ű�鼭 �Ź� �ٸ� �÷��̾�� ���� ����� �ϰ� �迭�� ���̷� ������ �迭 ���̸� ����� �ʰ� �Ѵ�.
        sfxCursor = (sfxCursor + 1) % sfxPlayer.Length;
    }

	void LateUpdate()
	{
        scoreText.text = score.ToString();
	}

	void Update()
	{
        if (Input.GetButtonDown("Cancel"))
        {
            Application.Quit();
        }
	}
}
