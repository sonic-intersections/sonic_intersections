﻿using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;

namespace Intersect.Client.Framework.Gwen.Control;


/// <summary>
///     Tree control node.
/// </summary>
public partial class TreeNode : Base
{

    public const int TREE_INDENTATION = 14;

    private bool mRoot;

    private bool mSelectable;

    private bool mSelected;

    protected Button mTitle;

    protected Button mToggleButton;

    protected TreeControl mTreeControl;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TreeNode" /> class.
    /// </summary>
    /// <param name="parent">Parent control.</param>
    public TreeNode(Base parent) : base(parent)
    {
        mToggleButton = new TreeToggleButton(this);
        mToggleButton.SetBounds(0, 0, 15, 15);
        mToggleButton.Toggled += OnToggleButtonPress;

        mTitle = new TreeNodeLabel(this);
        mTitle.Dock = Pos.Top;
        mTitle.Margin = new Margin(16, 0, 0, 0);
        mTitle.DoubleClicked += OnDoubleClickName;
        mTitle.Clicked += OnClickName;

        _innerPanel = new Base(this);
        _innerPanel.Dock = Pos.Top;
        _innerPanel.Height = 100;
        _innerPanel.Margin = new Margin(TREE_INDENTATION, 1, 0, 0);
        _innerPanel.Hide();

        mRoot = parent is TreeControl;
        mSelected = false;
        mSelectable = true;

        Dock = Pos.Top;
    }

    /// <summary>
    ///     Indicates whether this is a root node.
    /// </summary>
    public bool IsRoot
    {
        get => mRoot;
        set => mRoot = value;
    }

    /// <summary>
    ///     Parent tree control.
    /// </summary>
    public TreeControl TreeControl
    {
        get => mTreeControl;
        set => mTreeControl = value;
    }

    /// <summary>
    ///     Determines whether the node is selectable.
    /// </summary>
    public bool IsSelectable
    {
        get => mSelectable;
        set => mSelectable = value;
    }

    /// <summary>
    ///     Indicates whether the node is selected.
    /// </summary>
    public bool IsSelected
    {
        get => mSelected;
        set
        {
            if (!IsSelectable)
            {
                return;
            }

            if (IsSelected == value)
            {
                return;
            }

            mSelected = value;

            if (mTitle != null)
            {
                mTitle.ToggleState = value;
            }

            if (SelectionChanged != null)
            {
                SelectionChanged.Invoke(this, EventArgs.Empty);
            }

            // propagate to root parent (tree)
            if (mTreeControl != null && mTreeControl.SelectionChanged != null)
            {
                mTreeControl.SelectionChanged.Invoke(this, EventArgs.Empty);
            }

            if (value)
            {
                if (Selected != null)
                {
                    Selected.Invoke(this, EventArgs.Empty);
                }

                if (mTreeControl != null && mTreeControl.Selected != null)
                {
                    mTreeControl.Selected.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                if (Unselected != null)
                {
                    Unselected.Invoke(this, EventArgs.Empty);
                }

                if (mTreeControl != null && mTreeControl.Unselected != null)
                {
                    mTreeControl.Unselected.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    /// <summary>
    ///     Node's label.
    /// </summary>
    public string Text
    {
        get => mTitle.Text;
        set => mTitle.Text = value;
    }

    public IEnumerable<TreeNode> SelectedChildren
    {
        get
        {
            var trees = new List<TreeNode>();

            foreach (var child in Children)
            {
                var node = child as TreeNode;
                if (node == null)
                {
                    continue;
                }

                trees.AddRange(node.SelectedChildren);
            }

            if (this.IsSelected)
            {
                trees.Add(this);
            }

            return trees;
        }
    }

    /// <summary>
    ///     Invoked when the node label has been pressed.
    /// </summary>
    public event GwenEventHandler<EventArgs> LabelPressed;

    /// <summary>
    ///     Invoked when the node's selected state has changed.
    /// </summary>
    public event GwenEventHandler<EventArgs> SelectionChanged;

    /// <summary>
    ///     Invoked when the node has been selected.
    /// </summary>
    public event GwenEventHandler<EventArgs> Selected;

    /// <summary>
    ///     Invoked when the node has been unselected.
    /// </summary>
    public event GwenEventHandler<EventArgs> Unselected;

    /// <summary>
    ///     Invoked when the node has been expanded.
    /// </summary>
    public event GwenEventHandler<EventArgs> Expanded;

    /// <summary>
    ///     Invoked when the node has been collapsed.
    /// </summary>
    public event GwenEventHandler<EventArgs> Collapsed;

    /// <summary>
    /// Renders the control using the specified skin.
    /// </summary>
    /// <param name="skin">The skin to use.</param>
    protected override void Render(Skin.Base skin)
    {
        // Calculate the height of the tree node
        var treeNodeHeight = CalculateTreeNodeHeight();

        // Draw the tree node using the specified skin.
        skin.DrawTreeNode(
            this, _innerPanel.IsVisible, IsSelected, treeNodeHeight, mTitle.TextRight,
            (int)(mToggleButton.Y + mToggleButton.Height * 0.5f), _innerPanel.Bottom, mTreeControl == Parent
        ); // IsRoot

        // Invalidate the tree node.
        this.Invalidate();
    }

    /// <summary>
    /// Calculates the height of tree node.
    /// </summary>
    private int CalculateTreeNodeHeight()
    {
        var height = mTitle.Height;

        if (_innerPanel.Children.Count > 0)
        {
            height = _innerPanel.Children.Last().Y + height;
        }
        else if (height == 0)
        {
            height = _innerPanel.Height;
        }

        return height;
    }

    /// <summary>
    ///     Lays out the control's interior according to alignment, padding, dock etc.
    /// </summary>
    /// <param name="skin">Skin to use.</param>
    protected override void Layout(Skin.Base skin)
    {
        if (mToggleButton != null)
        {
            if (mTitle != null)
            {
                mToggleButton.SetPosition(0, (mTitle.Height - mToggleButton.Height) * 0.5f);
            }

            if (_innerPanel.Children.Count == 0)
            {
                mToggleButton.Hide();
                mToggleButton.ToggleState = false;
                _innerPanel.Hide();
            }
            else
            {
                mToggleButton.Show();
                _innerPanel.SizeToChildren(false, true);
            }
        }

        base.Layout(skin);
    }

    /// <summary>
    ///     Function invoked after layout.
    /// </summary>
    /// <param name="skin">Skin to use.</param>
    protected override void PostLayout(Skin.Base skin)
    {
        if (SizeToChildren(false, true))
        {
            InvalidateParent();
        }
    }

    /// <summary>
    ///     Adds a new child node.
    /// </summary>
    /// <param name="label">Node's label.</param>
    /// <returns>Newly created control.</returns>
    public TreeNode AddNode(string label)
    {
        var node = new TreeNode(this);
        node.Text = label;

        return node;
    }

    /// <summary>
    ///     Opens the node.
    /// </summary>
    public void Open()
    {
        _innerPanel.Show();
        if (mToggleButton != null)
        {
            mToggleButton.ToggleState = true;
        }

        if (Expanded != null)
        {
            Expanded.Invoke(this, EventArgs.Empty);
        }

        if (mTreeControl != null && mTreeControl.Expanded != null)
        {
            mTreeControl.Expanded.Invoke(this, EventArgs.Empty);
        }

        Invalidate();
    }

    /// <summary>
    ///     Closes the node.
    /// </summary>
    public void Close()
    {
        _innerPanel.Hide();
        if (mToggleButton != null)
        {
            mToggleButton.ToggleState = false;
        }

        if (Collapsed != null)
        {
            Collapsed.Invoke(this, EventArgs.Empty);
        }

        if (mTreeControl != null && mTreeControl.Collapsed != null)
        {
            mTreeControl.Collapsed.Invoke(this, EventArgs.Empty);
        }

        Invalidate();
    }

    /// <summary>
    ///     Opens the node and all child nodes.
    /// </summary>
    public void ExpandAll()
    {
        Open();
        foreach (var child in Children)
        {
            var node = child as TreeNode;
            if (node == null)
            {
                continue;
            }

            node.ExpandAll();
        }
    }

    /// <summary>
    ///     Clears the selection on the node and all child nodes.
    /// </summary>
    public void UnselectAll()
    {
        IsSelected = false;
        if (mTitle != null)
        {
            mTitle.ToggleState = false;
        }

        foreach (var child in Children)
        {
            var node = child as TreeNode;
            if (node == null)
            {
                continue;
            }

            node.UnselectAll();
        }
    }

    /// <summary>
    ///     Handler for the toggle button.
    /// </summary>
    /// <param name="control">Event source.</param>
    protected virtual void OnToggleButtonPress(Base control, EventArgs args)
    {
        if (mToggleButton.ToggleState)
        {
            Open();
        }
        else
        {
            Close();
        }
    }

    /// <summary>
    ///     Handler for label double click.
    /// </summary>
    /// <param name="control">Event source.</param>
    protected virtual void OnDoubleClickName(Base control, EventArgs args)
    {
        if (!mToggleButton.IsVisible)
        {
            return;
        }

        mToggleButton.Toggle();
    }

    /// <summary>
    ///     Handler for label click.
    /// </summary>
    /// <param name="control">Event source.</param>
    protected virtual void OnClickName(Base control, EventArgs args)
    {
        if (LabelPressed != null)
        {
            LabelPressed.Invoke(this, EventArgs.Empty);
        }

        IsSelected = !IsSelected;
    }

    public void SetImage(GameTexture texture, string fileName = "")
    {
        mTitle.SetImage(texture, fileName, Button.ControlState.Normal);
    }

    protected override void OnChildAdded(Base child)
    {
        var node = child as TreeNode;
        if (node != null)
        {
            node.TreeControl = mTreeControl;

            if (mTreeControl != null)
            {
                mTreeControl.OnNodeAdded(node);
            }
        }

        base.OnChildAdded(child);
    }

    public override event GwenEventHandler<ClickedEventArgs> Clicked
    {
        add { mTitle.Clicked += delegate(Base sender, ClickedEventArgs args) { value(this, args); }; }
        remove { mTitle.Clicked -= delegate(Base sender, ClickedEventArgs args) { value(this, args); }; }
    }

    public override event GwenEventHandler<ClickedEventArgs> DoubleClicked
    {
        add
        {
            if (value != null)
            {
                mTitle.DoubleClicked += delegate(Base sender, ClickedEventArgs args) { value(this, args); };
            }
        }
        remove { mTitle.DoubleClicked -= delegate(Base sender, ClickedEventArgs args) { value(this, args); }; }
    }

    public override event GwenEventHandler<ClickedEventArgs> RightClicked
    {
        add { mTitle.RightClicked += delegate(Base sender, ClickedEventArgs args) { value(this, args); }; }
        remove { mTitle.RightClicked -= delegate(Base sender, ClickedEventArgs args) { value(this, args); }; }
    }

    public override event GwenEventHandler<ClickedEventArgs> DoubleRightClicked
    {
        add
        {
            if (value != null)
            {
                mTitle.DoubleRightClicked += delegate(Base sender, ClickedEventArgs args) { value(this, args); };
            }
        }
        remove
        {
            mTitle.DoubleRightClicked -= delegate(Base sender, ClickedEventArgs args) { value(this, args); };
        }
    }

}
