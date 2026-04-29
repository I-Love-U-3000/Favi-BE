namespace Favi_BE.Modules.Moderation.Domain;

public enum AdminActionType
{
    Unknown = 0,
    BanUser = 1,
    UnbanUser = 2,
    WarnUser = 3,
    ResolveReport = 4,
    DeleteContent = 5,
    ExportData = 6
}
