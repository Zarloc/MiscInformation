using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using System.Drawing;

namespace MiscInformation
{
    public class MiscInformationSettings : ISettings
    {
        public MiscInformationSettings()
        {
            BackgroundColor = Color.FromArgb(120, 0, 0, 0);
            AreaTextColor = Color.FromArgb(140, 200, 255, 140);
            XphTextColor = Color.FromArgb(220, 190, 130, 220);
            XphGetLeft = Color.FromArgb(220, 190, 130, 220);
            TimeLeftColor = Color.FromArgb(220, 190, 130, 220);
            FpsTextColor = Color.FromArgb(220, 190, 130, 220);
            TimerTextColor = Color.FromArgb(220, 190, 130, 220);
            LatencyTextColor = Color.FromArgb(220, 190, 130, 220);
        }

        public ToggleNode Enable { get; set; } = new ToggleNode(true);
        public RangeNode<int> DrawXOffset { get; set; } = new RangeNode<int>(0, -150, 150);
        public ColorNode BackgroundColor { get; set; }
        public ColorNode AreaTextColor { get; set; }
        public ColorNode XphTextColor { get; set; }
        public ColorNode XphGetLeft { get; set; }
        public ColorNode TimeLeftColor { get; set; }
        public ColorNode FpsTextColor { get; set; }
        public ColorNode TimerTextColor { get; set; }
        public ColorNode LatencyTextColor { get; set; }
    }
}
