using Mirror;
/// <summary>
/// Used to keep track of how much ammo is in the clip of the gun when in the player's inventory.
/// </summary>
public class InventoryGun
{
    public int AmmoInClip;
    public int Id;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public InventoryGun()
    {
        AmmoInClip = 0;
        Id = -1;
    }

    public InventoryGun(int ammoInClip, int id)
    {
        AmmoInClip = ammoInClip;
        Id = id;
    }

    public override bool Equals(object obj)
    {
        InventoryGun other = obj as InventoryGun;

        if (other == null)
            return false;

        return other.Id == Id;
    }

    public override int GetHashCode()
    {
        int hash = 13;
        hash = (hash * 7) + Id.GetHashCode();
        return hash;
    }

    public void Deserialize(NetworkReader reader)
    {
        AmmoInClip = reader.ReadInt32();
        Id = reader.ReadInt32();
    }

    public void Serialize(NetworkWriter writer)
    {
        writer.WriteInt32(AmmoInClip);
        writer.WriteInt32(Id);
    }
}

public static class InventoryGunReaderWriter
{
    public static void WriteInventoryGun(this NetworkWriter writer, InventoryGun gun) 
    {
        writer.WriteInt32(gun.AmmoInClip);
        writer.WriteInt32(gun.Id);
    }

    public static InventoryGun ReadInventoryGun(this NetworkReader reader)
    {
        return new InventoryGun(reader.ReadInt32(), reader.ReadInt32());
    }
}