using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    public enum SocketKind
    {
        Relic = 0,
        Red = 1,
        Blue = 2,
        Green = 3,
        Prismatic = 4
    }

    [CreateAssetMenu(fileName = "UniqueWeaponConfig", menuName = "Game/Meta/Unique Weapon Config")]
    public sealed class UniqueWeaponConfig : ContentConfig
    {
        [SerializeField] private CharacterConfig _character;
        [SerializeField] private string _displayName;
        [SerializeField] private string _subtitle;
        [SerializeField] private int _menuOrder;
        [SerializeField] private bool _comingLater;
        [SerializeField] private Sprite _art;

        [Header("Combat, relative to the hero's starting weapon")]
        [SerializeField, Min(0.01f)] private float _damageScale = 1f;
        [SerializeField, Min(0.01f)] private float _attackSpeedScale = 1f;

        [Header("Progression")]
        [SerializeField, Min(0)] private int _forgeCost;
        [SerializeField, Min(1)] private int _maxLevel = 10;
        [SerializeField, Min(0f)] private float _upgradeCostBase = 40f;
        [SerializeField, Min(0f)] private float _upgradeCostExponent = 1.35f;
        [SerializeField] private float _damagePerLevel = 0.03f;

        [SerializeField] private WeaponSocket[] _sockets = Array.Empty<WeaponSocket>();
        [SerializeField] private WeaponTrait[] _traits = Array.Empty<WeaponTrait>();

        public CharacterConfig Character => _character;
        public string DisplayName => _displayName;
        public string Subtitle => _subtitle;
        public int MenuOrder => _menuOrder;
        public bool ComingLater => _comingLater;
        public Sprite Art => _art;
        public float DamageScale => _damageScale;
        public float AttackSpeedScale => _attackSpeedScale;
        public int ForgeCost => _forgeCost;
        public bool IsStarting => _forgeCost == 0 && _comingLater == false;
        public int MaxLevel => _maxLevel;
        public float DamagePerLevel => _damagePerLevel;
        public IReadOnlyList<WeaponSocket> Sockets => _sockets ?? Array.Empty<WeaponSocket>();
        public IReadOnlyList<WeaponTrait> Traits => _traits ?? Array.Empty<WeaponTrait>();

        public int UpgradeCost(int currentLevel)
        {
            return Mathf.RoundToInt(_upgradeCostBase * Mathf.Pow(currentLevel, _upgradeCostExponent));
        }

        public float LevelDamageBonus(int level)
        {
            return _damagePerLevel * Mathf.Max(0, level - 1);
        }

        public WeaponSocket FindSocket(string socketId)
        {
            IReadOnlyList<WeaponSocket> sockets = Sockets;

            for (int index = 0; index < sockets.Count; index++)
            {
                if (sockets[index] != null && sockets[index].Id == socketId)
                    return sockets[index];
            }

            return null;
        }

        public void Validate(List<string> errors)
        {
            if (_character == null)
                errors.Add($"{Id}: no character");

            MetaIds.ValidateUnique(Id + " sockets", Sockets, socket => socket?.Id, errors);
            MetaIds.ValidateUnique(Id + " traits", Traits, trait => trait?.Id, errors);

            int relicSockets = 0;

            foreach (WeaponSocket socket in Sockets)
            {
                if (socket != null && socket.Kind == SocketKind.Relic)
                    relicSockets++;
            }

            if (relicSockets > 1)
                errors.Add($"{Id}: {relicSockets} relic sockets, at most one is allowed");

            foreach (WeaponTrait trait in Traits)
                trait?.Effect.Validate($"{Id} trait '{trait.Id}'", errors);
        }
    }

    [Serializable]
    public sealed class WeaponSocket
    {
        [SerializeField] private string _id;
        [SerializeField] private SocketKind _kind;
        [SerializeField, Min(1)] private int _unlockLevel = 1;
        [SerializeField] private Vector2 _position;

        public string Id => _id;
        public SocketKind Kind => _kind;
        public int UnlockLevel => _unlockLevel;
        public Vector2 Position => _position;

        public bool Accepts(GemColor color)
        {
            switch (_kind)
            {
                case SocketKind.Prismatic:
                    return true;
                case SocketKind.Red:
                    return color == GemColor.Red;
                case SocketKind.Blue:
                    return color == GemColor.Blue;
                case SocketKind.Green:
                    return color == GemColor.Green;
                default:
                    return false;
            }
        }
    }

    [Serializable]
    public sealed class WeaponTrait
    {
        [SerializeField] private string _id;
        [SerializeField, Min(1)] private int _unlockLevel = 1;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private MetaEffect _effect = new MetaEffect();

        public string Id => _id;
        public int UnlockLevel => _unlockLevel;
        public string DisplayName => _displayName;
        public string Description => _description;
        public MetaEffect Effect => _effect ?? new MetaEffect();
    }
}
