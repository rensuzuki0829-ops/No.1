using UnityEngine;
using TMPro; // TextMeshProを使う場合に必要です

/// <summary>
/// プレイヤーの「速度」と「重量」を、一定時間だけ変化させるための管理スクリプトです。
/// このスクリプトは Player（ボール）にアタッチして使います。
/// アイテム側（PowerUpItem）から、下の ApplySpeed / ApplyWeight を呼んでもらいます。
/// </summary>
public class PlayerStatus : MonoBehaviour
{
    [Header("UI（任意）")]
    // 効果の残り時間を表示したい場合に、TextMeshProのUIを入れます。使わない場合は空でOKです。
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("安全のための制限")]
    // 重量が0や極端に小さい値になると物理挙動が壊れるため、最小値を決めておきます。
    [SerializeField] private float minMass = 0.1f;

    // 速度を変えるために、PlayerControllerを参照します。
    private PlayerController playerController;

    // 重量（mass）を変えるために、Rigidbodyを参照します。
    private Rigidbody playerRigidbody;

    // アイテムを取る前の「元の値」を覚えておくための変数です。効果が切れたらここへ戻します。
    private float baseSpeed;
    private float baseMass;

    // 効果の残り時間（秒）です。0より大きい間だけ効果がかかっています。
    private float speedEffectTimer;
    private float weightEffectTimer;

    // 現在かかっている倍率です。表示（UI）用に持っています。
    private float currentSpeedRate = 1f;
    private float currentWeightRate = 1f;

    private void Awake()
    {
        // 同じGameObjectに付いているPlayerControllerとRigidbodyを取得します。
        playerController = GetComponent<PlayerController>();
        playerRigidbody = GetComponent<Rigidbody>();

        // 取得できなかった場合は、設定ミスに気づけるように警告を出します。
        if (playerController == null)
        {
            Debug.LogWarning($"{name}: PlayerControllerが見つかりません。速度アイテムは効きません。", this);
        }
        if (playerRigidbody == null)
        {
            Debug.LogWarning($"{name}: Rigidbodyが見つかりません。重量アイテムは効きません。", this);
        }

        // 「元の値」を最初に保存しておきます。これが効果終了後に戻る値になります。
        baseSpeed = playerController != null ? playerController.speed : 0f;
        baseMass = playerRigidbody != null ? playerRigidbody.mass : 1f;
    }

    private void Update()
    {
        // 速度効果の残り時間を減らし、0以下になったら元の速度に戻します。
        if (speedEffectTimer > 0f)
        {
            speedEffectTimer -= Time.deltaTime;
            if (speedEffectTimer <= 0f)
            {
                ResetSpeed();
            }
        }

        // 重量効果の残り時間を減らし、0以下になったら元の重さに戻します。
        if (weightEffectTimer > 0f)
        {
            weightEffectTimer -= Time.deltaTime;
            if (weightEffectTimer <= 0f)
            {
                ResetWeight();
            }
        }

        // 残り時間などを画面に表示します（UIを設定していない場合は何もしません）。
        UpdateStatusText();
    }

    /// <summary>
    /// 速度に倍率をかけて、指定した秒数だけ効果を持続させます。
    /// rate が 1より大きい → スピードアップ / 1より小さい → スピードダウン
    /// </summary>
    public void ApplySpeed(float rate, float duration)
    {
        if (playerController == null)
        {
            return;
        }

        // 今すでに出ている勢い（Rigidbodyの速度）にも、同じ比率をかけてその場で調整します。
        // AddForceは「今後どれだけ加速しやすいか」しか変えないため、これをしないと
        // スピードダウンのアイテムを取っても、既に付いている勢いはそのまま残ってしまいます。
        // （0で割ってしまわないよう、比率の分母は最小値でガードします）
        ApplyVelocityRatio(rate / Mathf.Max(currentSpeedRate, 0.01f));

        // 「元の速度 × 倍率」で計算します。今の速度に掛けると、重ねがけでどんどん増えてしまうためです。
        currentSpeedRate = rate;
        playerController.speed = baseSpeed * rate;

        // 効果時間をセットし直します（新しいアイテムを取ると時間がリセットされます）。
        speedEffectTimer = duration;
    }

    /// <summary>
    /// Rigidbodyの水平方向の速度（x, z）に倍率をかけます。
    /// 上下方向（y）は重力やジャンプに関わるため変更しません。
    /// </summary>
    private void ApplyVelocityRatio(float ratio)
    {
        if (playerRigidbody == null)
        {
            return;
        }

        Vector3 velocity = playerRigidbody.velocity;
        velocity.x *= ratio;
        velocity.z *= ratio;
        playerRigidbody.velocity = velocity;
    }

    /// <summary>
    /// 重量（Rigidbodyのmass）に倍率をかけて、指定した秒数だけ効果を持続させます。
    /// rate が 1より大きい → 重くなる（動き出しが鈍く、止まりにくい）
    /// rate が 1より小さい → 軽くなる（すぐ加速するが、吹き飛びやすい）
    /// </summary>
    public void ApplyWeight(float rate, float duration)
    {
        if (playerRigidbody == null)
        {
            return;
        }

        // 元の重さを基準に計算し、軽くなりすぎないように最小値でストップさせます。
        currentWeightRate = rate;
        playerRigidbody.mass = Mathf.Max(baseMass * rate, minMass);

        // 効果時間をセットし直します。
        weightEffectTimer = duration;
    }

    /// <summary>
    /// 速度を元に戻します（効果終了時に呼ばれます）。
    /// </summary>
    private void ResetSpeed()
    {
        // 効果が切れて元の倍率(1倍)に戻るときも、今の勢いを同じ比率で調整します。
        ApplyVelocityRatio(1f / Mathf.Max(currentSpeedRate, 0.01f));

        speedEffectTimer = 0f;
        currentSpeedRate = 1f;

        if (playerController != null)
        {
            playerController.speed = baseSpeed;
        }
    }

    /// <summary>
    /// 重量を元に戻します（効果終了時に呼ばれます）。
    /// </summary>
    private void ResetWeight()
    {
        weightEffectTimer = 0f;
        currentWeightRate = 1f;

        if (playerRigidbody != null)
        {
            playerRigidbody.mass = baseMass;
        }
    }

    /// <summary>
    /// いま効果中の内容と残り時間を、UIテキストに表示します。
    /// </summary>
    private void UpdateStatusText()
    {
        // UIを設定していない場合は、表示処理をスキップします。
        if (statusText == null)
        {
            return;
        }

        string text = "";

        // 速度効果が残っていれば、倍率と残り秒数を文字列に足します。
        if (speedEffectTimer > 0f)
        {
            string label = currentSpeedRate >= 1f ? "スピードUP" : "スピードDOWN";
            text += $"{label} x{currentSpeedRate:F1} ({speedEffectTimer:F1}s)\n";
        }

        // 重量効果が残っていれば、同じように文字列に足します。
        if (weightEffectTimer > 0f)
        {
            string label = currentWeightRate >= 1f ? "重量UP" : "重量DOWN";
            text += $"{label} x{currentWeightRate:F1} ({weightEffectTimer:F1}s)";
        }

        statusText.text = text;
    }
}