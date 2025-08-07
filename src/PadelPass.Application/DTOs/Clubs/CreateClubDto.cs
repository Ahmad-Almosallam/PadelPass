using System.ComponentModel.DataAnnotations;

namespace PadelPass.Application.DTOs.Clubs;

public class CreateClubDto
{
    public string Name { get; set; }

    public string Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}