using System;
using Amplitude.Xml;

public class KaijuRelocationMessage : BlackboardMessage
{
	public KaijuRelocationMessage() : base(BlackboardLayerID.Empire)
	{
		this.KaijuGUID = GameEntityGUID.Zero;
		this.TargetPosition = WorldPosition.Invalid;
	}

	public KaijuRelocationMessage(GameEntityGUID kaijuGUID, WorldPosition targetPosition) : base(BlackboardLayerID.Empire)
	{
		this.KaijuGUID = kaijuGUID;
		this.TargetPosition = targetPosition;
	}

	public GameEntityGUID KaijuGUID { get; set; }

	public WorldPosition TargetPosition { get; set; }

	public override void ReadXml(XmlReader reader)
	{
		this.KaijuGUID = new GameEntityGUID(reader.GetAttribute<ulong>("KaijuGUID"));
		this.TargetPosition = new WorldPosition(reader.GetAttribute<short>("PositionRow"), reader.GetAttribute<short>("PositionColumn"));
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<ulong>("KaijuGUID", this.KaijuGUID);
		writer.WriteAttributeString<short>("PositionRow", this.TargetPosition.Row);
		writer.WriteAttributeString<short>("PositionColumn", this.TargetPosition.Column);
		base.WriteXml(writer);
	}
}
