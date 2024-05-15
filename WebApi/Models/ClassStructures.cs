using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;

namespace WebApi.Models
{
    public class ClassStructures
    {
        public List<SingleUser> Users { get; set; }
    }
    public class SingleUser
    {
        public ApiAdmin Admins { get; set; }
        public ApiConductor Conductors { get; set; }
        public ApiParent Parents { get; set; }
        public ApiStudent Students { get; set; }
        public string userRole { get; set; }
    }

    public class ApiAdmin
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Gender { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class ApiConductor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class ApiParent
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public int ChildrenEnroll { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class ApiStudent
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public string RegNo { get; set; }
        public string Contact { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string PassStatus { get; set; }
        public string PassExpiry { get; set; }
        public int TotalJourneys { get; set; }
        public int RemainingJourneys { get; set; }
        public int ParentId { get; set; }
        public int UserId { get; set; }
        public int PassId { get; set; }
    }
    public class ChildTimings
    {
        public string Pickup_Checkin { get; set; }
        public string Pickup_Checkout { get; set; }
        public string Dropup_Checkin { get; set; }
        public string Dropup_Checkout { get; set; }
    }

    public class Location
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
    }
    public class BusLocation
    {
        public int BusId { get; set;}
        public Location Cords { get; set; }
    }
    public class ApiNotification
    {
        public int Id { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public int UserID { get; set; }
        public int NotificationRead { get; set; }
    }
    public class ApiFavStops
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Timing { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public int Route { get; set; }
    }
    public class ApiStops
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Timing { get; set; }
        public string Logitude { get; set; }
        public string Latitude { get; set; }
        public int Route { get; set; }

    }
    public class AllHistory
    {
        public List<ApiTravel> travelHistory { get; set; }
        public List<ApiStart> startHistory { get; set; }
        public List<ApiReach> reachHistory { get; set; }
    }
    public class ApiTravel
    {
        public int Id { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Type { get; set; }
        public int PassId { get; set; }
        public int StudentId { get; set; }
        public int BusId { get; set; }
        public int RouteId { get; set; }
        public int StopId { get; set; }
    }
    public class ApiStart
    {
        public int Id { get; set; }
        public String Date { get; set; }
        public String Time { get; set; }
        public int BusId { get; set; }
        public int RouteId { get; set; }
    }
    public class ApiReach
    {
        public int Id { get; set; }
        public String Date { get; set; }
        public String Time { get; set; }
        public int BusId { get; set; }
        public int RouteId { get; set; }
        public int StopId { get; set; }
    }
    public class ApiChangePassword
    {
        public int id { get; set; }
        public string oldPassword { get; set; }
        public string newPassword { get; set; }
    }
}