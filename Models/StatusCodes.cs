namespace RestauranteAPI.Models;

public static class V2StatusCodes
{
    public const string Pending = "PENDING";
    public const string Active = "ACTIVE";
    public const string Completed = "COMPLETED";
    public const string Cancelled = "CANCELLED";
    public const string Waiting = "WAITING";
    public const string Assigned = "ASSIGNED";

    public static string ReservationFromLegacyName(string name) => name switch
    {
        "Pending" => Pending,
        "Active" => Active,
        "Completed" => Completed,
        "Cancelled" => Cancelled,
        _ => throw new ArgumentException($"Unknown legacy reservation status: {name}.")
    };
}
