namespace FinGrow.Api.Controllers;

using Contracts;
using Extensions;
using Application.Common;
using Application.Features.Notifications.Channels.ChangeNotificationChannel;
using Application.Features.Notifications.Channels.ListNotificationChannels;
using Application.Features.Notifications.CountUnreadNotifications;
using Application.Features.Notifications.ListNotifications;
using Application.Features.Notifications.MarkAllNotificationsAsRead;
using Application.Features.Notifications.MarkNotificationAsRead;
using Application.Features.Notifications.Types.ChangeNotificationType;
using Application.Features.Notifications.Types.ListNotificationTypes;
using Application.Features.Notifications.Types.ListCompanyNotificationTypes;
using Application.Features.Notifications.Types.ChangeCompanyNotificationType;
using Application.Interfaces;
using Domain.Enums;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] NotificationQueryParameters parameters, CancellationToken cancellationToken) =>
        (await sender.Send(
            new ListNotificationsQuery(Recipient, parameters.UnreadOnly, parameters.PageNumber, parameters.PageSize),
            cancellationToken)).ToActionResult();

    [HttpGet("unread-count")]
    public async Task<IActionResult> CountUnread(CancellationToken cancellationToken) =>
        (await sender.Send(new CountUnreadNotificationsQuery(Recipient), cancellationToken)).ToActionResult();

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken cancellationToken) =>
        (await sender.Send(new MarkNotificationAsReadCommand(Recipient, notificationId), cancellationToken)).ToActionResult();

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken) =>
        (await sender.Send(new MarkAllNotificationsAsReadCommand(Recipient), cancellationToken)).ToActionResult();

    [HttpGet("channels")]
    [Authorize(Roles = Rol.Empleado)]
    public async Task<IActionResult> ListChannels(CancellationToken cancellationToken) =>
        (await sender.Send(new ListNotificationChannelsQuery(currentUser.UserId!.Value), cancellationToken)).ToActionResult();

    [HttpPut("channels/{channel}")]
    [Authorize(Roles = Rol.Empleado)]
    public async Task<IActionResult> ChangeChannel(
        NotificationChannel channel,
        ChangeNotificationChannelRequest request,
        CancellationToken cancellationToken) =>
        (await sender.Send(
            new ChangeNotificationChannelCommand(currentUser.UserId!.Value, channel, request.IsEnabled),
            cancellationToken)).ToActionResult();

    [HttpGet("types")]
    public async Task<IActionResult> ListTypes(CancellationToken cancellationToken) =>
        currentUser.Role == Rol.Empresa
            ? (await sender.Send(new ListCompanyNotificationTypesQuery(currentUser.UserId!.Value), cancellationToken)).ToActionResult()
            : (await sender.Send(new ListNotificationTypesQuery(currentUser.UserId!.Value), cancellationToken)).ToActionResult();

    [HttpPut("types/{type}")]
    public async Task<IActionResult> ChangeType(
        NotificationType type,
        ChangeNotificationTypeRequest request,
        CancellationToken cancellationToken) =>
        currentUser.Role == Rol.Empresa
            ? (await sender.Send(
                new ChangeCompanyNotificationTypeCommand(
                    currentUser.UserId!.Value,
                    type,
                    request.IsEnabled,
                    request.Cadence ?? NotificationCadence.Monthly,
                    request.Threshold),
                cancellationToken)).ToActionResult()
            : (await sender.Send(
                new ChangeNotificationTypeCommand(currentUser.UserId!.Value, type, request.IsEnabled),
                cancellationToken)).ToActionResult();

    private NotificationRecipient Recipient =>
        currentUser.Role == Rol.Empresa
            ? NotificationRecipient.Company(currentUser.UserId!.Value)
            : NotificationRecipient.Employee(currentUser.UserId!.Value);
}
