using UnityEngine;

public class SpiderInputTest : MonoBehaviour
{
	private SpiderAgent _spider;

	private void Start() => _spider = GetComponent<SpiderAgent>();

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit))
			{
				_spider.MoveTo(hit.point);
			}
		}
	}
}