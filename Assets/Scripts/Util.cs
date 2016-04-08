using UnityEngine;
using System.Collections;

public class Util {

	public static Vector3 Hermite(
		Vector3 value1,
		Vector3 tangent1,
		Vector3 value2,
		Vector3 tangent2,
		float amount)
	{
		var squared = amount * amount;
		var cubed = amount * squared;
		var part1 = ((2.0f * cubed) - (3.0f * squared)) + 1.0f;
		var part2 = (-2.0f * cubed) + (3.0f * squared);
		var part3 = (cubed - (2.0f * squared)) + amount;
		var part4 = cubed - squared;

		return new Vector3(
			(((value1.x * part1) + (value2.x * part2)) + (tangent1.x * part3)) + (tangent2.x * part4),
			(((value1.y * part1) + (value2.y * part2)) + (tangent1.y * part3)) + (tangent2.y * part4),
			(((value1.z * part1) + (value2.z * part2)) + (tangent1.z * part3)) + (tangent2.z * part4));
	}
}
