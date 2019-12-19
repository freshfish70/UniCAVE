using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sets the minimum, maximum and current value to TextMesh PRO
/// text fields. Values are assigned on Start() and current value text
/// is updated with slider onValueChanged event.
/// </summary>
[RequireComponent(typeof(Slider))]
public class SliderVisualValueController : MonoBehaviour
{
	[SerializeField]
	private TMP_Text minValue;

	[SerializeField]
	private TMP_Text maxValue;

	[SerializeField]
	private TMP_Text currentValue;

	private Slider slider;

	private void Start()
	{
		try
		{
			this.slider = this.GetComponent<Slider>();
			this.slider.onValueChanged.AddListener(SetCurrentValue);
			this.SetCurrentValue(this.slider.value);
			this.SetMinMaxValues();
		}
		catch (UnityException e)
		{

		}
	}

	/// <summary>
	/// Sets the min and max values to text elements
	/// </summary>
	private void SetMinMaxValues()
	{
		this.minValue.SetText(this.slider.minValue.ToString());
		this.maxValue.SetText(this.slider.maxValue.ToString());
	}

	/// <summary>
	/// Clears event listeners
	/// </summary>
	private void OnDestroy()
	{
		this.slider.onValueChanged.RemoveAllListeners();
	}

	/// <summary>
	/// Sets the current slider value
	/// </summary>
	/// <param name="value">Current slider value</param>
	private void SetCurrentValue(float value)
	{
		this.currentValue.SetText(Math.Round(value, 3).ToString());
	}

}
