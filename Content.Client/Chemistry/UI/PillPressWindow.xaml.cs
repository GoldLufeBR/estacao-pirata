using System.Linq;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Chemistry.UI
{
    /// <summary>
    /// Client-side UI used to control a <see cref="SharedPillPressComponent"/>
    /// </summary>
    [GenerateTypedNameReferences]
    public sealed partial class PillPressWindow : DefaultWindow
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        public readonly Button[] PillTypeButtons;

        private const string PillsRsiPath = "/Textures/Objects/Specific/Chemistry/pills.rsi";

        /// <summary>
        /// Create and initialize the chem master UI client-side. Creates the basic layout,
        /// actual data isn't filled in until the server sends data about the chem master.
        /// </summary>
        public PillPressWindow()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            // Pill type selection buttons, in total there are 20 pills.
            // Pill rsi file should have states named as pill1, pill2, and so on.
            var resourcePath = new ResourcePath(PillsRsiPath);
            var pillTypeGroup = new ButtonGroup();
            PillTypeButtons = new Button[20];
            for (uint i = 0; i < PillTypeButtons.Length; i++)
            {
                // For every button decide which stylebase to have
                // Every row has 10 buttons
                String styleBase = StyleBase.ButtonOpenBoth;
                uint modulo = i % 10;
                if (i > 0 && modulo == 0)
                    styleBase = StyleBase.ButtonOpenRight;
                else if (i > 0 && modulo == 9)
                    styleBase = StyleBase.ButtonOpenLeft;
                else if (i == 0)
                    styleBase = StyleBase.ButtonOpenRight;

                // Generate buttons
                PillTypeButtons[i] = new Button
                {
                    Access = AccessLevel.Public,
                    StyleClasses = { styleBase },
                    MaxSize = (42, 28),
                    Group = pillTypeGroup
                };

                // Generate buttons textures
                var specifier = new SpriteSpecifier.Rsi(resourcePath, "pill" + (i + 1));
                TextureRect pillTypeTexture = new TextureRect
                {
                    Texture = specifier.Frame0(),
                    TextureScale = (1.75f, 1.75f),
                    Stretch = TextureRect.StretchMode.KeepCentered,
                };

                PillTypeButtons[i].AddChild(pillTypeTexture);
                Grid.AddChild(PillTypeButtons[i]);
            }

            PillDosage.InitDefaultButtons();
            PillNumber.InitDefaultButtons();

            // Ensure label length is within the character limit.
            LabelLineEdit.IsValid = s => s.Length <= SharedPillPress.LabelMaxLength;
        }

        /// <summary>
        /// Update the UI state when new state data is received from the server.
        /// </summary>
        /// <param name="state">State data sent by the server.</param>
        public void UpdateState(BoundUserInterfaceState state)
        {
            var castState = (PillPressBoundUserInterfaceState) state;
            UpdatePanelInfo(castState);

            var output = castState.OutputContainerInfo;

            InputEjectButton.Disabled = castState.InputContainerInfo is null;
            OutputEjectButton.Disabled = output is null;

            CreatePillButton.Disabled = output is null || output.HoldsReagents;

            var remainingCapacity = output is null ? 0 : (output.MaxVolume - output.CurrentVolume).Int();
            var holdsReagents = output?.HoldsReagents ?? false;
            var pillNumberMax = holdsReagents ? 0 : remainingCapacity;

            PillTypeButtons[castState.SelectedPillType].Pressed = true;
            PillNumber.IsValid = x => x >= 0 && x <= pillNumberMax;
            PillDosage.IsValid = x => x > 0 && x <= castState.PillDosageLimit;

            if (PillNumber.Value > pillNumberMax)
                PillNumber.Value = pillNumberMax;
        }

        /// <summary>
        /// Update the container, buffer, and packaging panels.
        /// </summary>
        /// <param name="state">State data for the dispenser.</param>
        private void UpdatePanelInfo(PillPressBoundUserInterfaceState state)
        {
            BuildContainerUI(InputContainerInfo, state.InputContainerInfo, true);
            BuildContainerUI(OutputContainerInfo, state.OutputContainerInfo, false);
        }

        private void BuildContainerUI(Control control, PillPressContainerInfo? info, bool addPillPressReagentButtons)
        {
            control.Children.Clear();

            if (info is null)
            {
                control.Children.Add(new Label
                {
                    Text = Loc.GetString("chem-master-window-no-container-loaded-text")
                });
            }
            else
            {
                // Name of the container and its fill status (Ex: 44/100u)
                control.Children.Add(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        new Label {Text = $"{info.DisplayName}: "},
                        new Label
                        {
                            Text = $"{info.CurrentVolume}/{info.MaxVolume}",
                            StyleClasses = {StyleNano.StyleClassLabelSecondaryColor}
                        }
                    }
                });

                var contents = info.Contents
                    .Select(lineItem =>
                    {
                        if (!info.HoldsReagents)
                            return (lineItem.Id, lineItem.Id, lineItem.Quantity);

                        // Try to get the prototype for the given reagent. This gives us its name.
                        _prototypeManager.TryIndex(lineItem.Id, out ReagentPrototype? proto);
                        var name = proto?.LocalizedPhysicalDescription
                                   ?? Loc.GetString("chem-master-window-unknown-reagent-text");

                        return (name, lineItem.Id, lineItem.Quantity);

                    })
                    .OrderBy(r => r.Item1);

                foreach (var (name, id, quantity) in contents)
                {
                    var inner = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            new Label { Text = $"{name}: " },
                            new Label
                            {
                                Text = $"{quantity}u",
                                StyleClasses = { StyleNano.StyleClassLabelSecondaryColor },
                            }
                        }
                    };

                    control.Children.Add(inner);
                }

            }
        }

        public String LabelLine
        {
            get
            {
                return LabelLineEdit.Text;
            }
            set
            {
                LabelLineEdit.Text = value;
            }
        }
    }
}
