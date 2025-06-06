﻿namespace PadelPass.Application.DTOs.Clubs;

public class ClubDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}