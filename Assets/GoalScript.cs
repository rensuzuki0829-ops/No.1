using UnityEngine;

/// <summary>
/// ゴール地点のスクリプトです。
/// 「フラグ」タグの付いたオブジェクトが、シーンから全部無くなっていたらクリアになります。
/// （フラグは取ると Destroy されるので、残り0個 = 全部集めた、という判定です）
/// </summary>
public class GoalScript : MonoBehaviour
{
    // クリアしたときに表示するテキストなどのオブジェクトです。
    public GameObject winnerLabelObject;

    // クリア条件になるフラグのタグ名です。
    [SerializeField]private string flagTag = "フラグ";
    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player"))
        {
            return;
        }

        // シーンに残っている「フラグ」を数え、0個ならクリアにします。
        if (GameObject.FindGameObjectsWithTag(flagTag).Length == 0)

        {
            winnerLabelObject.SetActive(true);
        }
    }
}
