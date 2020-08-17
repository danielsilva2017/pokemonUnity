using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores and loads the overworld state.
/// </summary>
public class OverworldManager : MonoBehaviour
{
    public PlayerLogic Player;
    public List<NPC> Characters;
    public List<Item> Items;

    // Start is called before the first frame update
    void Start()
    {
        var overworldInfo = SceneInfo.GetOverworldInfo();

        if (overworldInfo == null) return;

        Player.transform.position = overworldInfo.PlayerPosition;
        Player.FaceDirection(overworldInfo.PlayerDirection);

        // set all defeated trainers
        for (var i = 0; i < overworldInfo.Characters.Count; i++)
        {
            var npc = overworldInfo.Characters[i];
            if (npc.IsDefeated)
                Characters[i].IsDefeated = true;
        }

        // destroy all collected items
        for (var i = 0; i < overworldInfo.Items.Count; i++)
        {
            var item = overworldInfo.Items[i];
            if (item.IsCollected)
                Destroy(Items[i].gameObject);
        }

        SceneInfo.DeleteBattleInfo();
        SceneInfo.DeleteOverworldInfo();
    }

    public void CreateSnapshot()
    {
        SceneInfo.SetOverworldInfo(Player, Characters, Items, gameObject.scene.buildIndex);
    }

}
