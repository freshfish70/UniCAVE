using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Responsible for binding the visual GUI calibration tools
/// with the <code>RealtimeCalibrator</code>.
/// </summary>
public class VisualRealtimeCalibrationBinder : MonoBehaviour
{
	[SerializeField]
	private RealtimeCalibrator realtimeCalibrator;

	[Header("Vertices")]
	#region Vertices
	[SerializeField]
	private Slider selectionSize;
	[SerializeField]
	private Slider Fallof;
	[SerializeField]
	private Slider Delta;
	[SerializeField]
	private Toggle showVertices;
	#endregion

	[Header("Edgeblend")]
	#region Edgeblend
	[SerializeField]
	private Slider topBlend;
	[SerializeField]
	private Slider rightBlend;
	[SerializeField]
	private Slider bottomBlend;
	[SerializeField]
	private Slider leftBlend;

	#endregion
	void Start()
	{
		if (realtimeCalibrator == null)
		{
			throw new MissingComponentException("Requires RealtimeCalibrator in the scene!");
		}

		this.RegisterEvents();
		this.showVertices.SetIsOnWithoutNotify(false);
	}

	private void RegisterEvents()
	{
		selectionSize.onValueChanged.AddListener(SetSelectionSize);
		Fallof.onValueChanged.AddListener(SetFallofValue);
		Delta.onValueChanged.AddListener(SetDeltaValue);
		showVertices.onValueChanged.AddListener(SetDisplayVertices);

		topBlend.onValueChanged.AddListener(SetTopBlend);
		rightBlend.onValueChanged.AddListener(SetRightBlend);
		bottomBlend.onValueChanged.AddListener(SetBottomBlend);
		leftBlend.onValueChanged.AddListener(SetLeftBlend);
	}

	private void OnDestroy()
	{
		selectionSize.onValueChanged.RemoveAllListeners();
		Fallof.onValueChanged.RemoveAllListeners();
		Delta.onValueChanged.RemoveAllListeners();
		showVertices.onValueChanged.RemoveAllListeners();

		topBlend.onValueChanged.RemoveAllListeners();
		rightBlend.onValueChanged.RemoveAllListeners();
		bottomBlend.onValueChanged.RemoveAllListeners();
		leftBlend.onValueChanged.RemoveAllListeners();
	}

	private void SetSelectionSize(float size)
	{
		Debug.Log(size);
		this.realtimeCalibrator.SetGridSelectSize(Mathf.FloorToInt(size - 1));
	}

	public void SetSelectionSizeState(float size)
	{
	}

	private void SetFallofValue(float fallof)
	{
		this.realtimeCalibrator.SetFallof(fallof);
	}

	public void SetFallofValueState(float fallof)
	{
	}

	private void SetDeltaValue(float delta)
	{
		this.realtimeCalibrator.SetVerticeDelta(delta);
	}

	public void SetDeltaValueState(float delta)
	{
	}

	private void SetDisplayVertices(bool toggle)
	{
		this.realtimeCalibrator.ToggleVertice(toggle);
	}

	private void SetTopBlend(float blend)
	{
		this.realtimeCalibrator.EdgeBlend(blend, Side.TOP);
	}

	public void SetTopBlendState(float blend)
	{
	}

	private void SetRightBlend(float blend)
	{
		this.realtimeCalibrator.EdgeBlend(blend, Side.RIGHT);
	}

	public void SetRightBlendState(float blend)
	{
	}

	private void SetBottomBlend(float blend)
	{
		this.realtimeCalibrator.EdgeBlend(blend, Side.BOTTOM);
	}

	public void SetBottomBlendState(float blend)
	{
	}
	private void SetLeftBlend(float blend)
	{
		this.realtimeCalibrator.EdgeBlend(blend, Side.LEFT);
	}
	public void SetLeftBlendState(float blend)
	{
	}



	// Update is called once per frame
	void Update()
	{

	}

}
