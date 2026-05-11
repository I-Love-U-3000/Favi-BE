using Favi_BE.Modules.Auth.Application.Contracts.WriteModels;
using Favi_BE.Modules.Auth.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.UploadAvatar;

public sealed record UploadAvatarCommand(
    Guid ProfileId,
    UploadedImageData Image
) : IRequest<SavedImageResult>;
