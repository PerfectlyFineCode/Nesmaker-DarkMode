using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace NesmakerDarkmode;

#pragma warning disable CS0649
public static class ControlExtensions
{
	public static void ForEach(this Control control, Action<Control> action)
	{
		if (control == null) return;
		action(control);
		Queue<Control> controlQueue = new Queue<Control>();
		controlQueue.Enqueue(control);
		while (controlQueue.Count > 0)
		{
			Control currentControl = controlQueue.Dequeue();
			foreach (Control child in currentControl.Controls)
			{
				action(child);
				controlQueue.Enqueue(child);
			}
		}
	}
		
	public static void AddChildControlsToQueue(Control control, Queue<Control> controlQueue)
	{
		foreach (Control child in control.Controls)
		{
			controlQueue.Enqueue(child);
		}
	}
}