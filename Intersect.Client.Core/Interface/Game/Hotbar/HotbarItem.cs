using Intersect.Client.Core.Controls;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.DescriptionWindows;
using Intersect.Client.Items;
using Intersect.Client.Localization;
using Intersect.Client.Spells;
using Intersect.Core;
using Intersect.Framework.Reflection;
using Intersect.GameObjects;
using Intersect.Localization;
using Intersect.Utilities;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Interface.Game.Hotbar;


public partial class HotbarItem
{
    private const int ItemXPadding = 4;
    private const int ItemYPadding = 4;

    private readonly ImagePanel _contentPanel;
    private readonly Label _cooldownLabel;
    private readonly Label _equipLabel;
    private readonly ImagePanel _icon;
    private readonly Label _keyLabel;

    private bool _canDrag;
    private long _clickTime;
    private Guid _currentId = Guid.Empty;
    private ItemBase? _currentItem = null;
    private SpellBase? _currentSpell = null;
    private Draggable _dragIcon;
    private bool _isDragging;
    private bool _isEquipped;
    private bool _isFaded;
    private readonly Base _hotbarWindow;
    private readonly int _hotbarSlotIndex;
    private ControlValue? _hotKey;
    private Item? _inventoryItem = null;
    private int _inventoryItemIndex = -1;
    private ItemDescriptionWindow? _itemDescWindow;
    private bool _mouseOver;
    private int _mouseX = -1;
    private int _mouseY = -1;
    private Label _quantityLabel;
    private Spell? _spellBookItem = null;
    private SpellDescriptionWindow? _spellDescWindow;
    private bool _textureLoaded;

    public HotbarItem(int hotbarSlotIndex, Base hotbarWindow)
    {
        _hotbarSlotIndex = hotbarSlotIndex;
        _hotbarWindow = hotbarWindow;

        _icon = new ImagePanel(hotbarWindow, $"HotbarContainer{hotbarSlotIndex}");

        // Content Panel is layered on top of the container (shows the Item or Spell Icon).
        _contentPanel = new ImagePanel(HotbarIcon, $"{nameof(HotbarIcon)}{_hotbarSlotIndex}");
        _contentPanel.HoverEnter += hotbarIcon_HoverEnter;
        _contentPanel.HoverLeave += hotbarIcon_HoverLeave;
        _contentPanel.RightClicked += hotbarIcon_RightClicked;
        _contentPanel.Clicked += hotbarIcon_Clicked;

        _equipLabel = new Label(_icon, nameof(_equipLabel) + _hotbarSlotIndex)
        {
            IsHidden = true,
            Text = Strings.Inventory.EquippedSymbol,
            TextColor = new Color(255, 255, 255, 255)
        };

        _quantityLabel = new Label(_icon, nameof(_quantityLabel) + _hotbarSlotIndex)
        {
            IsHidden = true,
            TextColor = new Color(255, 255, 255, 255)
        };

        _cooldownLabel = new Label(_icon, nameof(_cooldownLabel) + _hotbarSlotIndex)
        {
            IsHidden = true,
            TextColor = new Color(255, 255, 255, 255)
        };

        _keyLabel = new Label(_icon, $"HotbarLabel{hotbarSlotIndex}");
    }

    public ImagePanel HotbarIcon => _icon;

    public void Activate()
    {
        if (_currentId != Guid.Empty && Globals.Me != null)
        {
            if (_currentItem != null)
            {
                if (_inventoryItemIndex > -1)
                {
                    Globals.Me.TryUseItem(_inventoryItemIndex);
                }
            }
            else if (_currentSpell != null)
            {
                Globals.Me.TryUseSpell(_currentSpell.Id);
            }
        }
    }

    private void hotbarIcon_RightClicked(Base sender, ClickedEventArgs arguments)
    {
        if (Globals.Me == null)
        {
            return;
        }

        Globals.Me.AddToHotbar(_hotbarSlotIndex, -1, -1);
    }

    private void hotbarIcon_Clicked(Base sender, ClickedEventArgs arguments)
    {
        _clickTime = Timing.Global.MillisecondsUtc + 500;
    }

    private void hotbarIcon_HoverLeave(Base sender, EventArgs arguments)
    {
        _mouseOver = false;
        _mouseX = -1;
        _mouseY = -1;
        if (_itemDescWindow != null)
        {
            _itemDescWindow.Dispose();
            _itemDescWindow = null;
        }

        if (_spellDescWindow != null)
        {
            _spellDescWindow.Dispose();
            _spellDescWindow = null;
        }
    }

    private void hotbarIcon_HoverEnter(Base sender, EventArgs arguments)
    {
        if (InputHandler.MouseFocus != null || Globals.Me == null)
        {
            return;
        }

        _mouseOver = true;
        _canDrag = true;
        if (Globals.InputManager.MouseButtonDown(MouseButtons.Left))
        {
            _canDrag = false;

            return;
        }

        if (_currentItem != null && _inventoryItem != null)
        {
            if (_itemDescWindow != null)
            {
                _itemDescWindow.Dispose();
                _itemDescWindow = null;
            }

            var quantityOfItem = 1;

            if (_currentItem.IsStackable)
            {
                quantityOfItem = Globals.Me.GetQuantityOfItemInInventory(_currentItem.Id);
            }

            _itemDescWindow = new ItemDescriptionWindow(
                _currentItem, quantityOfItem, _hotbarWindow.X + (_hotbarWindow.Width / 2), _hotbarWindow.Y + _hotbarWindow.Height + 2,
                _inventoryItem.ItemProperties, _currentItem.Name, ""
            );
        }
        else if (_currentSpell != null)
        {
            if (_spellDescWindow != null)
            {
                _spellDescWindow.Dispose();
                _spellDescWindow = null;
            }

            _spellDescWindow = new SpellDescriptionWindow(
                _currentSpell.Id, _hotbarWindow.X + (_hotbarWindow.Width / 2), _hotbarWindow.Y + _hotbarWindow.Height + 2
            );
        }
    }

    public FloatRect RenderBounds()
    {
        var rect = new FloatRect()
        {
            X = HotbarIcon.LocalPosToCanvas(new Point(0, 0)).X,
            Y = HotbarIcon.LocalPosToCanvas(new Point(0, 0)).Y,
            Width = HotbarIcon.Width,
            Height = HotbarIcon.Height
        };

        return rect;
    }

    public void Update()
    {
        if (Globals.Me == null || Controls.ActiveControls == null)
        {
            return;
        }

        // Check if the label should be changed
        var keybind = Controls.ActiveControls.ControlMapping[Control.Hotkey1 + _hotbarSlotIndex].Bindings[0];
        if (_hotKey == null || _hotKey.Modifier != keybind.Modifier || _hotKey.Key != keybind.Key)
        {
            var keyName = keybind.Key.GetName(isModifier: false).ToLowerInvariant();
            if (!Strings.Keys.KeyDictionary.TryGetValue(keyName, out var localizedKeyString))
            {
                localizedKeyString = keyName;
            }

            string assembledKeyText = localizedKeyString;

            var modifier = keybind.Modifier;
            if (modifier is not Keys.None)
            {
                var modifierName = modifier.GetName(isModifier: true).ToLowerInvariant();
                string modifierText = Strings.Keys.KeyDictionary.TryGetValue(modifierName, out var localizedModifierString)
                    ? localizedModifierString
                    : modifierName;
                assembledKeyText = Strings.Keys.KeyNameWithModifier.ToString(modifierText, assembledKeyText);
            }

            _keyLabel.SetText(assembledKeyText);

            _hotKey = keybind;
        }

        var slot = Globals.Me.Hotbar[_hotbarSlotIndex];
        var updateDisplay = _currentId != slot.ItemOrSpellId || _textureLoaded == false; // Update display if item changes or we dont have a texture for it.

        if (_currentId != slot.ItemOrSpellId)
        {
            _currentItem = null;
            _currentSpell = null;
            var itm = ItemBase.Get(slot.ItemOrSpellId);
            var spl = SpellBase.Get(slot.ItemOrSpellId);
            if (itm != null)
            {
                _currentItem = itm;
            }

            if (spl != null)
            {
                _currentSpell = spl;
            }

            _currentId = slot.ItemOrSpellId;
        }

        _spellBookItem = null;
        _inventoryItem = null;
        _inventoryItemIndex = -1;

        if (_currentItem != null)
        {
            var itmIndex = Globals.Me.FindHotbarItem(slot);
            if (itmIndex > -1)
            {
                _inventoryItemIndex = itmIndex;
                _inventoryItem = (Item)Globals.Me.Inventory[itmIndex];
            }
        }
        else if (_currentSpell != null)
        {
            var splIndex = Globals.Me.FindHotbarSpell(slot);
            if (splIndex > -1)
            {
                _spellBookItem = Globals.Me.Spells[splIndex] as Spell;
            }
        }

        if (_currentItem != null) //When it's an item
        {
            //We don't have it, and the icon isn't faded
            if (_inventoryItem == null && !_isFaded)
            {
                updateDisplay = true;
            }

            //We have it, and the equip icon doesn't match equipped status
            if (_inventoryItem != null && Globals.Me.IsEquipped(_inventoryItemIndex) != _isEquipped)
            {
                updateDisplay = true;
            }

            //We have it, and it's on cd
            if (_inventoryItem != null && Globals.Me.IsItemOnCooldown(_inventoryItemIndex))
            {
                updateDisplay = true;
            }

            //We have it, and it's on cd, and the fade is incorrect
            if (_inventoryItem != null && Globals.Me.IsItemOnCooldown(_inventoryItemIndex) != _isFaded)
            {
                updateDisplay = true;
            }

            //We have it, and the quantity label is incorrect
            var quantityText = Strings.FormatQuantityAbbreviated(Globals.Me.GetQuantityOfItemInInventory(_currentItem.Id));
            if (_inventoryItem != null && _quantityLabel.Text != quantityText)
            {
                _quantityLabel.Text = quantityText;
                updateDisplay = true;
            }
        }

        if (_currentSpell != null) //When it's a spell
        {
            //We don't know it, remove from hotbar right away!
            if (_spellBookItem == null)
            {
                Globals.Me.AddToHotbar(_hotbarSlotIndex, -1, -1);
                updateDisplay = true;
            }

            //Spell on cd
            if (_spellBookItem != null &&
                Globals.Me.GetSpellCooldown(_spellBookItem.Id) > Timing.Global.Milliseconds)
            {
                updateDisplay = true;
            }

            //Spell on cd and the fade is incorrect
            if (_spellBookItem != null &&
                Globals.Me.GetSpellCooldown(_spellBookItem.Id) > Timing.Global.Milliseconds != _isFaded)
            {
                updateDisplay = true;
            }
        }

        if (updateDisplay) //Item on cd and fade is incorrect
        {
            if (_currentItem != null)
            {
                _contentPanel.Show();
                _contentPanel.Texture = Globals.ContentManager.GetTexture(
                    Framework.Content.TextureType.Item, _currentItem.Icon
                );

                _equipLabel.IsHidden = true;
                _quantityLabel.IsHidden = true;
                _cooldownLabel.IsHidden = true;

                if (_inventoryItemIndex > -1)
                {
                    _isFaded = Globals.Me.IsItemOnCooldown(_inventoryItemIndex);
                    _isEquipped = Globals.Me.IsEquipped(_inventoryItemIndex);

                    if (_isFaded)
                    {
                        _cooldownLabel.IsHidden = false;
                        _cooldownLabel.Text = TimeSpan
                            .FromMilliseconds(Globals.Me.GetItemRemainingCooldown(_inventoryItemIndex))
                            .WithSuffix();
                    }
                }
                else
                {
                    _equipLabel.IsHidden = true;
                    _quantityLabel.IsHidden = true;
                    _cooldownLabel.IsHidden = true;
                    _isEquipped = false;
                    _isFaded = true;
                }

                _equipLabel.IsHidden = !_isEquipped || _inventoryItemIndex < 0;
                _quantityLabel.IsHidden = !_currentItem.Stackable || _inventoryItemIndex < 0;
                _cooldownLabel.IsHidden = !_isFaded || _inventoryItemIndex < 0;

                _textureLoaded = true;
            }
            else if (_currentSpell != null)
            {
                _contentPanel.Show();
                _contentPanel.Texture = Globals.ContentManager.GetTexture(
                    Framework.Content.TextureType.Spell, _currentSpell.Icon
                );

                _equipLabel.IsHidden = true;
                _quantityLabel.IsHidden = true;
                _cooldownLabel.IsHidden = true;
                if (_spellBookItem != null)
                {
                    var spellSlot = Globals.Me.FindHotbarSpell(slot);
                    _isFaded = Globals.Me.IsSpellOnCooldown(spellSlot);
                    if (_isFaded)
                    {
                        _cooldownLabel.IsHidden = false;
                        var remaining = Globals.Me.GetSpellRemainingCooldown(spellSlot);
                        _cooldownLabel.Text = TimeSpan.FromMilliseconds(remaining).WithSuffix("0.0");
                    }
                }
                else
                {
                    _isFaded = true;
                }

                _textureLoaded = true;
                _isEquipped = false;
            }
            else
            {
                _contentPanel.Hide();
                _textureLoaded = true;
                _isEquipped = false;
                _equipLabel.IsHidden = true;
                _quantityLabel.IsHidden = true;
                _cooldownLabel.IsHidden = true;
            }

            if (_isFaded)
            {
                if (_currentSpell != null)
                {
                    _contentPanel.RenderColor = new Color(60, 255, 255, 255);
                }

                if (_currentItem != null)
                {
                    _contentPanel.RenderColor = new Color(60, _currentItem.Color.R, _currentItem.Color.G, _currentItem.Color.B);
                }
            }
            else
            {
                if (_currentSpell != null)
                {
                    _contentPanel.RenderColor = Color.White;
                }

                if (_currentItem != null)
                {
                    _contentPanel.RenderColor = _currentItem.Color;
                }
            }
        }

        if (_currentItem != null || _currentSpell != null)
        {
            if (!_isDragging)
            {
                _contentPanel.IsHidden = false;

                var equipLabelIsHidden = _currentItem == null || !Globals.Me.IsEquipped(_inventoryItemIndex) || _inventoryItemIndex < 0;
                _equipLabel.IsHidden = equipLabelIsHidden;

                var quantityLabelIsHidden = _currentItem is not { Stackable: true } || _inventoryItemIndex < 0;
                _quantityLabel.IsHidden = quantityLabelIsHidden;

                if (_mouseOver)
                {
                    if (!Globals.InputManager.MouseButtonDown(MouseButtons.Left))
                    {
                        _canDrag = true;
                        _mouseX = -1;
                        _mouseY = -1;
                        if (Timing.Global.MillisecondsUtc < _clickTime)
                        {
                            Activate();
                            _clickTime = 0;
                        }
                    }
                    else
                    {
                        if (_canDrag && Draggable.Active == null)
                        {
                            if (_mouseX == -1 || _mouseY == -1)
                            {
                                _mouseX = InputHandler.MousePosition.X - HotbarIcon.LocalPosToCanvas(new Point(0, 0)).X;
                                _mouseY = InputHandler.MousePosition.Y - HotbarIcon.LocalPosToCanvas(new Point(0, 0)).Y;
                            }
                            else
                            {
                                var xdiff = _mouseX -
                                            (InputHandler.MousePosition.X -
                                             HotbarIcon.LocalPosToCanvas(new Point(0, 0)).X);

                                var ydiff = _mouseY -
                                            (InputHandler.MousePosition.Y -
                                             HotbarIcon.LocalPosToCanvas(new Point(0, 0)).Y);

                                if (Math.Sqrt(Math.Pow(xdiff, 2) + Math.Pow(ydiff, 2)) > 5)
                                {
                                    _isDragging = true;
                                    _dragIcon = new Draggable(
                                        HotbarIcon.LocalPosToCanvas(new Point(0, 0)).X + _mouseX,
                                        HotbarIcon.LocalPosToCanvas(new Point(0, 0)).X + _mouseY, _contentPanel.Texture, _contentPanel.RenderColor
                                    );

                                    //SOMETHING SHOULD BE RENDERED HERE, RIGHT?
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (_dragIcon.Update())
                {
                    //Drug the item and now we stopped
                    _isDragging = false;
                    var dragRect = new FloatRect(
                        _dragIcon.X - ItemXPadding / 2, _dragIcon.Y - ItemYPadding / 2, ItemXPadding / 2 + 32,
                        ItemYPadding / 2 + 32
                    );

                    float bestIntersect = 0;
                    var bestIntersectIndex = -1;

                    if (Interface.GameUi.Hotbar.RenderBounds().IntersectsWith(dragRect))
                    {
                        for (var i = 0; i < Options.Instance.Player.HotbarSlotCount; i++)
                        {
                            if (Interface.GameUi.Hotbar.Items[i].RenderBounds().IntersectsWith(dragRect))
                            {
                                if (FloatRect.Intersect(Interface.GameUi.Hotbar.Items[i].RenderBounds(), dragRect)
                                        .Width *
                                    FloatRect.Intersect(Interface.GameUi.Hotbar.Items[i].RenderBounds(), dragRect)
                                        .Height >
                                    bestIntersect)
                                {
                                    bestIntersect =
                                        FloatRect.Intersect(
                                                Interface.GameUi.Hotbar.Items[i].RenderBounds(), dragRect
                                            )
                                            .Width *
                                        FloatRect.Intersect(
                                                Interface.GameUi.Hotbar.Items[i].RenderBounds(), dragRect
                                            )
                                            .Height;

                                    bestIntersectIndex = i;
                                }
                            }
                        }

                        if (bestIntersectIndex > -1 && bestIntersectIndex != _hotbarSlotIndex)
                        {
                            Globals.Me.HotbarSwap(_hotbarSlotIndex, (byte)bestIntersectIndex);
                        }
                    }

                    _dragIcon.Dispose();
                }
                else
                {
                    _contentPanel.IsHidden = true;
                    _equipLabel.IsHidden = true;
                    _quantityLabel.IsHidden = true;
                    _cooldownLabel.IsHidden = true;
                }
            }
        }
    }
}
