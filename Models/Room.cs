using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;

namespace PickPlace.Api.Models;

public class Room
{
    public int? Id { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string Facilities { get; set; } = string.Empty;
    public string Issues { get; set; } = string.Empty;
    public int Capacity { get; set; } = 0;
    
    public bool IsDeleted { get; set; } = false;
}
