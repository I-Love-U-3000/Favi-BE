using Favi_BE.Modules.Auth.Application.Contracts.WriteModels;
using Favi_BE.Modules.Auth.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.UploadPoster;

public sealed record UploadPosterCommand(
    Guid ProfileId,
    UploadedImageData Image
) : IRequest<SavedImageResult>;
