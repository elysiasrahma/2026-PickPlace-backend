namespace PickPlace.Api.Models;

public class Room
{
    public string Id { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string Facilities { get; set; } = string.Empty;
    public string Issues { get; set; } = string.Empty;
    public int Capacity { get; set; } = 0;
    
}
