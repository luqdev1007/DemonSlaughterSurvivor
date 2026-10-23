using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    public enum HeroTreeNodeType
    {
        Start = 0,
        Class = 1,
        PerkModifier = 2,
        Keystone = 3,
        Golden = 4
    }

    [CreateAssetMenu(fileName = "HeroTreeConfig", menuName = "Game/Meta/Hero Tree Config")]
    public sealed class HeroTreeConfig : ContentConfig
    {
        [SerializeField] private CharacterConfig _character;
        [SerializeField] private string _displayName;
        [SerializeField] private HeroTreeNode[] _nodes = Array.Empty<HeroTreeNode>();
        [SerializeField] private HeroTreeBranchLabel[] _branchLabels = Array.Empty<HeroTreeBranchLabel>();

        public CharacterConfig Character => _character;
        public string DisplayName => _displayName;
        public IReadOnlyList<HeroTreeNode> Nodes => _nodes ?? Array.Empty<HeroTreeNode>();
        public IReadOnlyList<HeroTreeBranchLabel> BranchLabels => _branchLabels ?? Array.Empty<HeroTreeBranchLabel>();

        public HeroTreeNode Find(string nodeId)
        {
            IReadOnlyList<HeroTreeNode> nodes = Nodes;

            for (int index = 0; index < nodes.Count; index++)
            {
                if (nodes[index] != null && nodes[index].Id == nodeId)
                    return nodes[index];
            }

            return null;
        }

        public void Validate(List<string> errors)
        {
            if (_character == null)
                errors.Add($"{Id}: no character");

            MetaIds.ValidateUnique(Id + " nodes", Nodes, node => node?.Id, errors);

            int starts = 0;

            foreach (HeroTreeNode node in Nodes)
            {
                if (node == null)
                    continue;

                string label = $"{Id} node '{node.Id}'";

                if (node.Type == HeroTreeNodeType.Start)
                    starts++;

                if (node.MaxRank < 1)
                    errors.Add($"{label}: max rank {node.MaxRank}");

                if (node.Costs.Count != node.MaxRank)
                    errors.Add($"{label}: {node.Costs.Count} costs for {node.MaxRank} ranks");

                foreach (string parent in node.Parents)
                {
                    if (Find(parent) == null)
                        errors.Add($"{label}: unknown parent '{parent}'");
                }

                if (string.IsNullOrEmpty(node.Excludes) == false)
                {
                    HeroTreeNode excluded = Find(node.Excludes);

                    if (excluded == null)
                        errors.Add($"{label}: unknown excluded node '{node.Excludes}'");
                    else if (excluded.Excludes != node.Id)
                        errors.Add($"{label}: excludes '{node.Excludes}', but that node does not exclude it back");
                }

                foreach (HeroTreeGate gate in node.Gates)
                {
                    foreach (string member in gate.AnyOf)
                    {
                        if (Find(member) == null)
                            errors.Add($"{label}: gate names unknown node '{member}'");
                    }
                }

                if (node.Gates.Count > 0 && (node.GatesRequired < 1 || node.GatesRequired > node.Gates.Count))
                    errors.Add($"{label}: needs {node.GatesRequired} of {node.Gates.Count} gates");

                node.Effect.Validate(label, errors);
            }

            if (starts != 1)
                errors.Add($"{Id}: {starts} start nodes, exactly one is required");
        }
    }

    [Serializable]
    public sealed class HeroTreeNode
    {
        [SerializeField] private string _id;
        [SerializeField] private HeroTreeNodeType _type;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private string _rankNote;
        [SerializeField, Min(1)] private int _maxRank = 1;
        [SerializeField] private int[] _costs = Array.Empty<int>();
        [SerializeField] private string[] _parents = Array.Empty<string>();
        [SerializeField] private string _excludes;
        [SerializeField] private HeroTreeGate[] _gates = Array.Empty<HeroTreeGate>();
        [SerializeField, Min(0)] private int _gatesRequired;
        [SerializeField] private PerkConfig _perk;
        [SerializeField] private Vector2 _position;
        [SerializeField] private MetaEffect _effect = new MetaEffect();

        public string Id => _id;
        public HeroTreeNodeType Type => _type;
        public string DisplayName => _displayName;
        public string Description => _description;
        public string RankNote => _rankNote;
        public int MaxRank => _maxRank;
        public IReadOnlyList<int> Costs => _costs ?? Array.Empty<int>();
        public IReadOnlyList<string> Parents => _parents ?? Array.Empty<string>();
        public string Excludes => _excludes;
        public IReadOnlyList<HeroTreeGate> Gates => _gates ?? Array.Empty<HeroTreeGate>();
        public int GatesRequired => _gatesRequired;
        public PerkConfig Perk => _perk;
        public Vector2 Position => _position;
        public MetaEffect Effect => _effect ?? new MetaEffect();

        public int Cost(int currentRank)
        {
            IReadOnlyList<int> costs = Costs;

            if (costs.Count == 0)
                return 0;

            return costs[Mathf.Clamp(currentRank, 0, costs.Count - 1)];
        }
    }

    [Serializable]
    public sealed class HeroTreeGate
    {
        [SerializeField] private string[] _anyOf = Array.Empty<string>();

        public IReadOnlyList<string> AnyOf => _anyOf ?? Array.Empty<string>();
    }

    [Serializable]
    public sealed class HeroTreeBranchLabel
    {
        [SerializeField] private string _text;
        [SerializeField] private Vector2 _position;

        public string Text => _text;
        public Vector2 Position => _position;
    }
}
