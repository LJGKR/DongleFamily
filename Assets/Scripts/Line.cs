using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line : MonoBehaviour
{
	public GameManager manager;


	void OnEnable()
	{
		transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition); ;
	}

	void Update()
    {
		if (manager.isOver)
			return;

		Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

		float leftBorder = -4.2f + manager.lastDongle.transform.localScale.x / 2f;
		float rightBorder = 4.2f - manager.lastDongle.transform.localScale.x / 2f;

		if (mousePos.x < leftBorder)
		{
			mousePos.x = leftBorder;
		}
		else if (mousePos.x > rightBorder)
		{
			mousePos.x = rightBorder;
		}

		mousePos.y = 0f;
		mousePos.z = 0;
		transform.position = Vector3.Lerp(transform.position, mousePos, 0.2f);
	}
}
