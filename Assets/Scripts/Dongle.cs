using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SearchService;

public class Dongle : MonoBehaviour
{
	public GameManager manager;
	public ParticleSystem effect;
	public int level;
    public bool isDrag = false;
	public bool isMerge;
	public bool isAttach;
	

	public Rigidbody2D rigid;
	Animator anim;
	CircleCollider2D circle;
	SpriteRenderer sprite;

	float deadTime;

	void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		circle = GetComponent<CircleCollider2D>();
		anim = GetComponent<Animator>();
		sprite = GetComponent<SpriteRenderer>();
	}

	void OnEnable()
	{
		anim.SetInteger("Level", level);
	}

	void OnDisable() //������Ʈ�� ��Ȱ��ȭ�� �� ����Ǵ� �Լ�
	{
		//���� �Ӽ� �ʱ�ȭ
		level = 0;
		isDrag = false;
		isMerge = false;
		isAttach = false;

		//���� Ʈ������ �ʱ�ȭ
		transform.localPosition = Vector3.zero;
		transform.localRotation = Quaternion.identity;
		transform.localScale = Vector3.zero;

		//���� ���� �ʱ�ȭ
		rigid.simulated = false;
		rigid.velocity = Vector2.zero;
		rigid.angularVelocity = 0;
		circle.enabled = true;
	}

	void Update()
    {
        if (isDrag)
        {
			Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			float leftBorder = -4.2f + transform.localScale.x / 2f;
			float rightBorder = 4.2f - transform.localScale.x / 2f;

			if (mousePos.x < leftBorder)
			{
				mousePos.x = leftBorder;
			}
			else if (mousePos.x > rightBorder)
			{
				mousePos.x = rightBorder;
			}

			mousePos.y = 7.5f;
			mousePos.z = 0;
			transform.position = Vector3.Lerp(transform.position, mousePos, 0.2f);
		}
	}

	public void Drag()
	{
        isDrag = true;
		manager.line.SetActive(true);
	}

	public void Drop()
	{
		isDrag = false;
        rigid.simulated = true;
		manager.line.SetActive(false);
	}

	void OnCollisionEnter2D(Collision2D collision)
	{
		StartCoroutine("AttachRoutine");
	}

	IEnumerator AttachRoutine()
	{
		if (isAttach)
		{
			yield break;
		}

		isAttach = true;
		manager.SfxPlay(GameManager.Sfx.Attach);

		yield return new WaitForSeconds(0.2f);

		isAttach = false;
	}

	void OnCollisionStay2D(Collision2D collision)
	{
		if (collision.gameObject.tag == "Dongle")
		{
			Dongle other = collision.gameObject.GetComponent<Dongle>();

			if(level == other.level && !isMerge && !other.isMerge && level < 7)
			{
				float meX = transform.position.x;
				float meY = transform.position.y;
				float otherX = other.transform.position.x;
				float otherY = other.transform.position.y;
				//�������� ���
				//1. ���� �Ʒ��� ��
				//2. ������ ���̿� ���� �������� ��
				if(meY < otherY || (meX > otherX && meY == otherY)) {
					//�ٸ� ���� �����
					other.Hide(transform.position);
					//�ڽ��� ������
					LevelUp();
				}
			}
		}
	}

	public void Hide(Vector3 targetPos)
	{
		isMerge = true;
		rigid.simulated = false;
		circle.enabled = false;

		if(targetPos == Vector3.up * 100)
		{
			EffectPlay();
		}

		StartCoroutine(HideRoutine(targetPos));
	}

	IEnumerator HideRoutine(Vector3 targetPos)
	{
		int frameCount = 0;

		while(frameCount < 20)
		{
			frameCount++;
			if(targetPos != Vector3.up * 100)
			{
				transform.position = Vector3.Lerp(transform.position, targetPos, 0.5f);
				//Lerp -> �����Լ�(�� �� A, B ������ ������ ä����)
			}
			else if(targetPos == Vector3.forward * 100)
			{
				transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 0.2f);
			}

			yield return null;
		}

		manager.score += (int)Mathf.Pow(2, level);
		//Pow(A,B) -> A�� B�� ���� = A�� B���� ���� 
		isMerge = false;
		gameObject.SetActive(false);
	}

	void LevelUp()
	{
		isMerge = true;

		rigid.velocity = Vector2.zero;
		rigid.angularVelocity = 0;

		StartCoroutine(LevelUpRoutine());
	}

	IEnumerator LevelUpRoutine()
	{
		yield return new WaitForSeconds(0.2f);

		anim.SetInteger("Level", level + 1);
		EffectPlay();
		manager.SfxPlay(GameManager.Sfx.LevelUp);

		yield return new WaitForSeconds(0.3f);
		level++;

		//Max -> �� �� �߿��� �׻� �� ū ���� ����
		//manager.maxLevel = Mathf.Max(level, manager.maxLevel);
		isMerge = false;
	}

	void OnTriggerStay2D(Collider2D collision)
	{
		if(collision.tag == "Finish")
		{
			deadTime += Time.deltaTime;

			if(deadTime > 2)
			{
				sprite.color = new Color(0.8f,0.2f,0.2f);
			}
			if(deadTime > 5)
			{
				manager.GameOver();
			}
		}
	}

	void OnTriggerExit2D(Collider2D collision)
	{
		if(collision.tag == "Finish")
		{
			deadTime = 0;
			sprite.color = Color.white;
		}
	}

	void EffectPlay()
	{
		effect.transform.position = transform.position;
		effect.transform.localScale = transform.localScale;
		effect.Play();
	}
}
