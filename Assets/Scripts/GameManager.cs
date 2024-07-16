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
	//사운드 채널링
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
		//이펙트 생성
		GameObject InstantEffectObj = Instantiate(effectPrefab, effectGroup);
		InstantEffectObj.name = "Effect " + effectPool.Count;
		ParticleSystem instantEffect = InstantEffectObj.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffect);

		//동글 생성
		GameObject instant = Instantiate(donglePrefab, dongleGroup);
		instant.name = "Dongle " + donglePool.Count;
		//생성한 오브젝트는 2번째로 받은 인수의 자식 오브젝트로 생성이 된다.
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
            if (!donglePool[poolCursor].gameObject.activeSelf){ //풀에 비활성화 되어 있는 동글이 있다면
                return donglePool[poolCursor]; //그 동글을 반환
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
            yield return null; //한 틱을 기다린다.

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
		//1. 쌓아놓은 모든 동글 가져오기
		Dongle[] dongles = GameObject.FindObjectsOfType<Dongle>();

		//2. 중간에 합쳐지는 걸 방지하기 위해 물리효과 제거
		for (int i = 0; i < dongles.Length; i++)
		{
            dongles[i].rigid.simulated = false;
		}

		//3. 하나씩 접근해서 지우기 + 점수 환전
		for (int i = 0; i < dongles.Length; i++)
		{
			dongles[i].Hide(Vector3.up * 100);
            yield return new WaitForSeconds(0.1f);
		}

        yield return new WaitForSeconds(1f);

        //최고 점수 갱신
        int maxScore = Mathf.Max(score, PlayerPrefs.GetInt("MaxScore"));
        PlayerPrefs.SetInt("MaxScore", maxScore);

        //게임오버 UI 표시
        subScoreText.text = "점수 : " + scoreText.text;
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

        //다른 사운드가 재생될 때 플레이어의 클립이 바뀌어 기존의 사운드가 중지될 수 있기에
        //다른 사운드 플레이어로 매번 이동시켜 사운드가 중단되지 않도록 한다.
        sfxPlayer[sfxCursor].Play();
        //커서를 증가시키면서 매번 다른 플레이어로 사운드 재생을 하고 배열의 길이로 나누어 배열 길이를 벗어나지 않게 한다.
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
