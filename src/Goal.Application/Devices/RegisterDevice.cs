using FluentValidation;
using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using Goal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Devices;

/// <summary>Registers (or reactivates) an FCM device token for the current user.</summary>
public record RegisterDeviceCommand(string FcmToken, DevicePlatform Platform) : IRequest<Result>;

/// <summary>Deactivates a device token (e.g. on logout) so pushes stop for that device.</summary>
public record UnregisterDeviceCommand(string FcmToken) : IRequest<Result>;

public class RegisterDeviceValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceValidator()
    {
        RuleFor(x => x.FcmToken).NotEmpty().MaximumLength(512);
    }
}

public class RegisterDeviceHandler : IRequestHandler<RegisterDeviceCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public RegisterDeviceHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db; _currentUser = currentUser; _clock = clock;
    }

    public async Task<Result> Handle(RegisterDeviceCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var existing = await _db.DeviceTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.FcmToken == cmd.FcmToken, ct);

        if (existing is not null)
        {
            existing.IsActive = true;
            existing.LastSeenAt = _clock.UtcNow;
        }
        else
        {
            _db.DeviceTokens.Add(new DeviceToken
            {
                UserId = userId,
                FcmToken = cmd.FcmToken,
                Platform = cmd.Platform,
                IsActive = true,
                LastSeenAt = _clock.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class UnregisterDeviceHandler : IRequestHandler<UnregisterDeviceCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UnregisterDeviceHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(UnregisterDeviceCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var token = await _db.DeviceTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.FcmToken == cmd.FcmToken, ct);
        if (token is not null)
        {
            token.IsActive = false;
            await _db.SaveChangesAsync(ct);
        }
        return Result.Success();
    }
}
