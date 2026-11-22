using ConstructionProjectManagement.Models;

namespace ConstructionProjectManagement.ViewModels;

public static class WbsStatusLabels
{
    public static string For(ProjectPhaseStatus status) => status switch
    {
        ProjectPhaseStatus.NotStarted => "Chưa bắt đầu",
        ProjectPhaseStatus.InProgress => "Đang thực hiện",
        ProjectPhaseStatus.Completed => "Hoàn thành",
        ProjectPhaseStatus.Archived => "Đã lưu trữ",
        _ => "Không xác định"
    };

    public static string For(WbsItemStatus status) => status switch
    {
        WbsItemStatus.NotStarted => "Chưa bắt đầu",
        WbsItemStatus.InProgress => "Đang thực hiện",
        WbsItemStatus.Completed => "Hoàn thành",
        WbsItemStatus.OnHold => "Tạm hoãn",
        WbsItemStatus.Archived => "Đã lưu trữ",
        _ => "Không xác định"
    };
}
