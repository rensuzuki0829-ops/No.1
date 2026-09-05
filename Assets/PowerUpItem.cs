using UnityEngine;

/// <summary>
/// 触れると、一定時間だけプレイヤーの「速度」または「重量」を変化させるアイテムです。
/// このスクリプトは、アイテムにしたいGameObject（球や箱など）にアタッチします。
/// ・Colliderの「Is Trigger」にチェックが必要です（無い場合は自動で付けます）。
/// ・プレイヤー側には PlayerStatus をアタッチし、タグを "Player" にしておきます。
/// </summary>
public class PowerUpItem : MonoBehaviour
{
    /// <summary>
    /// アイテムの種類です。Inspectorのプルダウンから選びます。
    /// </summary>
    public enum ItemType
    {
        None,       // 効果なし（ただの収集アイテム／フラグ用）
        SpeedUp,    // 速度アップ
        SpeedDown,  // 速度ダウン
        WeightUp,   // 重量アップ（重くなる）
        WeightDown  // 重量ダウン（軽くなる）
    }

    [Header("アイテム設定")]
    // どの種類のアイテムにするかを選びます。
    [SerializeField] private ItemType itemType = ItemType.SpeedUp;

    // 効果の強さ（倍率）です。
    // アップ系は 2 のように1より大きい値、ダウン系は 0.5 のように1より小さい値にします。
    [SerializeField] private float rate = 2.0f;

    // 効果が続く時間（秒）です。
    [SerializeField] private float duration = 5.0f;

    // プレイヤーだと判断するためのタグ名です。
    [SerializeField] private string playerTag = "Player";

    [Header("取得後の動作")]
    // trueにすると、一定時間後に同じ場所へ復活します。falseなら取ったら消えたままです。
    [SerializeField] private bool respawn = false;

    // 復活するまでの待ち時間（秒）です。respawnがtrueのときだけ使います。
    [SerializeField] private float respawnTime = 5.0f;

    [Header("見た目")]
    // trueにすると、アイテムの種類に合わせて色を自動で変えます（見分けやすくするため）。
    [SerializeField] private bool autoColor = true;

    // trueにすると、アイテムがその場でくるくる回ります。
    [SerializeField] private bool rotate = true;

    // 回転の速さ（1秒あたりの角度）です。
    [SerializeField] private float rotateSpeed = 90f;

    // 見た目を切り替えるために、自分のRendererとColliderを覚えておきます。
    private Renderer itemRenderer;
    private Collider itemCollider;

    private void Reset()
    {
        // Inspectorでスクリプトを追加した瞬間に、Colliderをトリガー化しておきます。
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        itemRenderer = GetComponent<Renderer>();
        itemCollider = GetComponent<Collider>();

        // Colliderが無いとプレイヤーとの接触を判定できないので、自動で追加します。
        if (itemCollider == null)
        {
            itemCollider = gameObject.AddComponent<SphereCollider>();
        }

        // すり抜けて「触れた」判定にするため、必ずトリガーにします。
        itemCollider.isTrigger = true;

        // 種類ごとに色を変えて、どのアイテムか見て分かるようにします。
        if (autoColor && itemRenderer != null)
        {
            // material（コピー）を書き換えることで、他のオブジェクトに影響しないようにします。
            itemRenderer.material.color = GetTypeColor();
        }
    }

    private void Update()
    {
        // アイテムを回転させて目立たせます。
        if (rotate)
        {
            transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 触れた相手がプレイヤーでなければ、何もしません。
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        // Noneの場合は効果を持たない、ただの収集アイテム（フラグなど）として扱います。
        if (itemType != ItemType.None)
        {
            // プレイヤーに付いている PlayerStatus を探します。
            // 子オブジェクトのColliderが当たる場合も考えて、親方向へも探します。
            PlayerStatus status = other.GetComponent<PlayerStatus>();
            if (status == null)
            {
                status = other.GetComponentInParent<PlayerStatus>();
            }

            // PlayerStatus が無い場合は効果をかけられないので、設定ミスとして警告を出します。
            if (status == null)
            {
                Debug.LogWarning($"{name}: PlayerにPlayerStatusがアタッチされていません。", this);
                return;
            }

            // 種類に応じて、速度か重量のどちらを変えるかを切り替えます。
            switch (itemType)
            {
                case ItemType.SpeedUp:
                case ItemType.SpeedDown:
                    status.ApplySpeed(rate, duration);
                    break;

                case ItemType.WeightUp:
                case ItemType.WeightDown:
                    status.ApplyWeight(rate, duration);
                    break;
            }
        }

        // 取ったあとの処理（消す／一定時間後に復活）を行います。
        if (respawn)
        {
            // 見た目と当たり判定だけをオフにして、時間が経ったら元に戻します。
            SetItemActive(false);
            Invoke(nameof(Respawn), respawnTime);
        }
        else
        {
            // 復活しない設定なら、アイテムそのものを削除します。
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// アイテムの見た目と当たり判定をまとめてオン／オフします。
    /// </summary>
    private void SetItemActive(bool isActive)
    {
        if (itemRenderer != null)
        {
            itemRenderer.enabled = isActive;
        }
        if (itemCollider != null)
        {
            itemCollider.enabled = isActive;
        }
    }

    /// <summary>
    /// アイテムを復活させます（Invokeから呼ばれます）。
    /// </summary>
    private void Respawn()
    {
        SetItemActive(true);
    }

    /// <summary>
    /// アイテムの種類ごとの色を返します。
    /// </summary>
    private Color GetTypeColor()
    {
        switch (itemType)
        {
            case ItemType.SpeedUp:
                return Color.cyan;    // 水色：速くなる
            case ItemType.SpeedDown:
                return Color.blue;    // 青　：遅くなる
            case ItemType.WeightUp:
                return Color.gray;    // 灰色：重くなる
            case ItemType.WeightDown:
                return Color.yellow;  // 黄色：軽くなる
            case ItemType.None:
            default:
                return Color.white;   // 白：効果なしの通常アイテム
        }
    }
}