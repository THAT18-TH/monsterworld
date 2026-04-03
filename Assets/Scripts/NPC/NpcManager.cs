using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterWorldLike.NPC
{
    public class NpcManager : MonoBehaviour
    {
        public static NpcManager Instance { get; private set; }
        public static event Action InstanceReady;

        [SerializeField] private List<NpcDefinition> npcs = new();

        public event Action<string, string> OnDialogueLine;

        private readonly Dictionary<string, NpcDefinition> defs = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            defs.Clear();
            foreach (var npc in npcs)
            {
                if (npc == null || string.IsNullOrWhiteSpace(npc.npcId))
                {
                    continue;
                }

                defs[npc.npcId] = npc;
            }

            InstanceReady?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void StartDialogue(string npcId)
        {
            if (!defs.TryGetValue(npcId, out var npc) || npc.dialogueLines == null)
            {
                return;
            }

            for (var i = 0; i < npc.dialogueLines.Length; i++)
            {
                var line = npc.dialogueLines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                OnDialogueLine?.Invoke(npc.displayName, line);
            }
        }
    }
}
