namespace FinGrow.Application.Common;

public static class SessionClaims
{
    // No "sid": el mapeo de claims de JwtBearer lo renombra a ClaimTypes.Sid.
    public const string SessionId = "session_id";
}
