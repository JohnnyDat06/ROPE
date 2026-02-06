using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Spider Chase Player", story: "Chase the [Target]", category: "Spider AI")]
public class SpiderChaseAction : Action
{
	[SerializeReference] public BlackboardVariable<Transform> Target;

	// Tham chiếu đến code di chuyển cũ của bạn
	private SpiderAgent _agent;

	protected override Status OnStart()
	{
		// Lấy component SpiderAgent từ GameObject đang chạy Graph này
		if (GameObject == null) return Status.Failure;
		_agent = GameObject.GetComponent<SpiderAgent>();

		if (_agent == null || Target.Value == null) return Status.Failure;
		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		if (Target.Value == null) return Status.Failure;

		// GỌI HÀM CỦA BẠN: Ra lệnh di chuyển đến vị trí Player
		_agent.MoveTo(Target.Value.position);

		// Kiểm tra điều kiện thoát (VD: Player chết hoặc quá xa) ở logic Graph, 
		// còn ở đây ta cứ trả về Running để nó rượt liên tục.
		return Status.Running;
	}

	protected override void OnEnd()
	{
		if (_agent != null) _agent.Stop();
	}
}