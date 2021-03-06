﻿using UnityEngine;

namespace Exodrifter.NodeGraph.DefaultNodes
{
	[Node(Name = "Repeat", Path = "Math/Repeat")]
	public class RepeatNode : BakedNode
	{
		[Description("A value.")]
		[SerializeField, Input("F")]
		internal float f = 0;

		[Description("The minimum value.")]
		[SerializeField, Input("Min")]
		internal float min = 0;

		[Description("The maximum value.")]
		[SerializeField, Input("Max")]
		internal float max = 1;

		[Description("The value <i>F</i> repeated between <i>[Min, Max]</i>.")]
		[SerializeField, Output("Result")]
		internal float result = 0;

		public override float InputWidth
		{
			get { return 40; }
		}

		#region Methods

		public override void Eval(GraphScope scope)
		{
			result = min + Mathf.Repeat(f, max - min);
		}

		#endregion
	}
}
