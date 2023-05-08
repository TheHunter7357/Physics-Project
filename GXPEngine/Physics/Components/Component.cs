using GXPEngine;
using System;

[Serializable]
public class Component : IRefreshable
{
	protected bool Enabled;
	public readonly GameObject Owner;
	public Component(GameObject owner)
	{
		Enabled = true;
		Owner = owner;
		Refresh();
	}
	public virtual void Refresh() => Settings.Setup.OnAfterStep += Update;
	protected virtual void Update() { }
}