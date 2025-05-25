namespace PadelPass.Core.Constants;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string User = "User";
    public const string ClubUser = "ClubUser";
    
    public static readonly string[] AllRoles = [SuperAdmin, Admin, User, ClubUser];
}