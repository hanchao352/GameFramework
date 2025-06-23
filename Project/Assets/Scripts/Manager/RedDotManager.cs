using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TreeNode
{
    public int Id { get; set; }
    public bool Value { get; set; }
    public int ForceRed { get; set; }
    public TreeNode Parent { get; set; }
    public Dictionary<int, TreeNode> Children { get; set; }

    public TreeNode(int id)
    {
        Id = id;
        Value = false;
        ForceRed = -1;
        Children = new Dictionary<int, TreeNode>();
    }

    public void AddChild(TreeNode child)
    {
        child.Parent = this;
        Children[child.Id] = child;
    }

    public TreeNode GetChild(params int[] nodes)
    {
        TreeNode node = this;

        foreach (var id in nodes)
        {
            if (!node.Children.TryGetValue(id, out node))
            {
                return null;
            }
        }

        return node;
    }
}

public class RedDotInfo
{
    public int RedDotCount { get; set; }
    public int NonRedDotCount { get; set; }
    public bool Value { get; set; }
    public int ForceRed { get; set; }
}

public class RedDotManager:SingletonManager<RedDotManager> , IGeneric
{
    private Dictionary<int, TreeNode> Nodes = new Dictionary<int, TreeNode>();

 

    public override void Initialize()
    {
        base.Initialize();
       
    }
    
    public override void Update(float time)
    {
        base.Update(time);
    }

    public override void Dispose()
    {
        base.Dispose();
    }



    public void SetValue(bool value, params int[] nodes)
    {
        if (nodes.Length == 0)
        {
            return;
        }

        var node = GetOrCreateNode(nodes);

        if (node.ForceRed != -1)
        {
            return;
        }

        node.Value = value;
        PropagateValue(node);
    }

    public bool GetValue(params int[] nodes)
    {
        if (nodes.Length == 0)
        {
            return false;
        }

        var node = GetOrCreateNode(nodes);
        return node.Value;
    }

    public void SetForceRed(int forceRed, params int[] nodes)
    {
        if (nodes.Length == 0)
        {
            return;
        }

        var node = GetOrCreateNode(nodes);
        node.ForceRed = forceRed;

        if (forceRed != -1)
        {
            node.Value = forceRed == 1;
            PropagateValue(node);
        }
    }

    public int GetForceRed(params int[] nodes)
    {
        if (nodes.Length == 0)
        {
            return -1;
        }

        var node = GetOrCreateNode(nodes);
        return node.ForceRed;
    }

    public RedDotInfo GetRedDotInfo(params int[] nodes)
    {
        if (nodes.Length == 0)
        {
            return null;
        }

        var node = GetOrCreateNode(nodes);

        var redDotInfo = new RedDotInfo
        {
            RedDotCount = CountRedDots(node),
            NonRedDotCount = CountNonRedDots(node),
            Value = node.Value,
            ForceRed = node.ForceRed
        };

        return redDotInfo;
    }

    public void Log(params int[] nodes)
    {
        if (nodes.Length == 0)
        {
            // If no nodes specified, log all root nodes
            foreach (var node in Nodes.Values)
            {
                LogNode(node, 0);
            }
        }
        else
        {
            // If nodes specified, log only the specified node
            if (Nodes.TryGetValue(nodes[0], out var node))
            {
                node = node.GetChild(nodes[1..]);

                if (node != null)
                {
                    LogNode(node, 0);
                }
            }
        }
    }

    private void LogNode(TreeNode node, int depth)
    {
        Console.WriteLine(new string(' ', depth * 2) + $"Node {node.Id}: Value={node.Value}, ForceRed={node.ForceRed}");

        foreach (var child in node.Children.Values)
        {
            LogNode(child, depth + 1);
        }
    }

    private TreeNode GetOrCreateNode(int[] nodes)
    {
        if (!Nodes.TryGetValue(nodes[0], out var node))
        {
            node = new TreeNode(nodes[0]);
            Nodes[nodes[0]] = node;
        }

        for (int i = 1; i < nodes.Length; i++)
        {
            if (!node.Children.TryGetValue(nodes[i], out var child))
            {
                child = new TreeNode(nodes[i]);
                node.AddChild(child);
            }

            node = child;
        }

        return node;
    }

    private void PropagateValue(TreeNode node)
    {
        bool value = node.ForceRed == -1 ? node.Value : node.ForceRed == 1;

        while (node.Parent != null)
        {
            node = node.Parent;

            bool newValue = node.ForceRed == -1 ? node.Children.Values.Any(x => x.Value) : node.ForceRed == 1;

            if (newValue == node.Value)
            {
                break;
            }

            node.Value = newValue;
        }
    }

    private int CountRedDots(TreeNode node)
    {
        int count = node.Value ? 1 : 0;

        foreach (var child in node.Children.Values)
        {
            count += CountRedDots(child);
        }

        return count;
    }

    private int CountNonRedDots(TreeNode node)
    {
        int count = !node.Value ? 1 : 0;

        foreach (var child in node.Children.Values)
        {
            count += CountNonRedDots(child);
        }

        return count;
    }
}
