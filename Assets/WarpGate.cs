using System.Collections.Generic;
using UnityEngine;

public class WarpGate : MonoBehaviour
{
    [Header("Warp")]
    //ワープ後にプレイヤーを移動させる目的地。Inspectorで空のGameObjectなどを指定する。

    [SerializeField]private Transform exitPoint;
    //ワープ対象として認識するタグ名です。このプロジェクトではプレイヤーに"player"タグをつけます。

    [SerializeField]private string playerTag = "Player";
    //連続で何度もワープしないようにする待ち時間です。
    [SerializeField]private float cooldown = 1f;

    //trueの場合、ワープ前のRigidbody速度をワープ後も維持します。
    [SerializeField]private bool preserveVelocity = true;

    //trueの場合、入り口から出口の向きに合わせて速度方向を回転させます。
    [SerializeField]private bool alignVelocityToExitForward = true;

    [Header("Portal Visual")]
    //trueの場合、モデル形状に沿った発光エフェクトと粒子を自動生成します。
    [SerializeField]private bool createVisuals = true;

    //色。
    [SerializeField]private Color portalColor = new Color(0.1f, 0.8f, 1f, 1f);

    //元モデルより大きくメッ発酵シュを重ねるための倍率
    [SerializeField] private float effectScaleOffset = 1.04f;

    //発酵メッシュが脈打つ大きさ
    [SerializeField] private float pulseAmount = 0.035f;

// 発光メッシュが脈打つ速さ
[SerializeField] private float pulseSpeed = 3.5f;

// メッシュ表面から出る粒子の発生量
[SerializeField] private float particleRate = 40f;

// 粒子1つあたりの大きさ
[SerializeField] private float particleSize = 0.06f;

// 自動生成した見た目用オブジェクトをまとめる親オブジェクト名
private const string VisualRootName = "__WarpGateVisual";

// 発光メッシュと粒子オブジェクトの名前に付ける接尾辞←What`s this
private const string AuraSuffix = "_WarpAura";
private const string ParticleSuffix = "_WarpParticles";

// ワープ対象ごとのクールダウン終了時刻を保存
private static readonly Dictionary<Transform, float> WarpLocks = new Dictionary<Transform, float>();

// 脈動アニメーションをかける発光メッシュの一覧
private readonly List<VisualPulse> pulseVisuals = new List<VisualPulse>();

private Transform visualRoot;
private Material auraMaterial;
private Material particleMaterial;

// 発酵メッシュのTransformと元のScaleをセットで保持
private class VisualPulse
{
    public Transform Transform;
    public Vector3 BaseScale;
}

private void Reset()
{
    // コンポーネント追加時にColliderをTrigger化して通過判定用にする
    Collider gateCollider = GetOrCreateGateCollider();
    gateCollider.isTrigger = true;
}

private void Awake()
{
    // 実行開始時にColliderをTrigger化して設定漏れを防ぐ
    Collider gateCollider = GetOrCreateGateCollider();
    gateCollider.isTrigger = true;

    if (createVisuals)
    {
        // モデル形状に沿ったエフェクトを生成
        EnsureVisuals();
    }
}

private Collider GetOrCreateGateCollider()
{
    // 既にColliderがある場合はそれを使う
    Collider gateCollider = GetComponent<Collider>();
    if (gateCollider != null)
    {
        return gateCollider;
    }

    // Colliderが無い場合はワープ判定用のBoxColliderを自動追加
    BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
    boxCollider.size = new Vector3(2.4f, 2.4f, 0.35f);
    return boxCollider;
}

private void Update()
{
    // sin波で倍率を作り発光メッシュをゆっくり拡大縮小
    float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
    for (int i = 0; i < pulseVisuals.Count; i++)
    {
        VisualPulse visualPulse = pulseVisuals[i];
        if (visualPulse.Transform != null)
        {
            visualPulse.Transform.localScale = visualPulse.BaseScale * pulse;
        }
    }
}
private void OnTriggerEnter(Collider other)
{
    // 出口が未設定の場合はワープできない、警告を出して終了
    if (exitPoint == null)
    {
        Debug.LogWarning($"{name}: WarpGate has no exit point.", this);
        return;
    }

    // 触れたColliderがワープ対象か確認
    Transform target = GetWarpTarget(other);
    if (target == null)
    {
        return;
    }

    // クールダウン中なら再ワープ不可。
    if (WarpLocks.TryGetValue(target, out float lockedUntil) && Time.time < lockedUntil)
    {
        return;
    }

    // 次にワープできる時刻を記録、実際に移動させる
    WarpLocks[target] = Time.time + cooldown;
    Teleport(target, other.attachedRigidbody);
}

private Transform GetWarpTarget(Collider other)
{
    // Rigidbody付きオブジェクトならRigidbodyのTransformを対象
    Rigidbody attachedRigidbody = other.attachedRigidbody;
    Transform target = attachedRigidbody != null ? attachedRigidbody.transform : other.transform;

    // 対象本体か触れたColliderのどちらかにPlayerタグがあればワープ対象
    if (target.CompareTag(playerTag) || other.CompareTag(playerTag))
    {
        return target;
    }

    return null;
}

private void Teleport(Transform target, Rigidbody targetRigidbody)
{
    // Rigidbodyがある場合はワープ前速度を一時保存
    Vector3 velocity = targetRigidbody != null ? targetRigidbody.velocity : Vector3.zero;

    // Transformを出口の位置と向きに合わせる
    target.position = exitPoint.position;
    target.rotation = exitPoint.rotation;

    if (targetRigidbody == null)
    {
        return;
    }

    // Rigidbody側にも位置と回転を反映して物理挙動とのズレを防ぐ
    targetRigidbody.position = exitPoint.position;
    targetRigidbody.rotation = exitPoint.rotation;

    // 速度を維持しない設定なら移動後に完全停止させる
    if (!preserveVelocity)
    {
        targetRigidbody.velocity = Vector3.zero;
        targetRigidbody.angularVelocity = Vector3.zero;
        return;
    }

    // 入口と出口の向きの差に合わせて速度方向も回転させる
    if (alignVelocityToExitForward && velocity.sqrMagnitude > 0.0001f)
    {
        Quaternion fromGateToExit = Quaternion.FromToRotation(transform.forward, exitPoint.forward);
        velocity = fromGateToExit * velocity;
    }

    // 最終的な速度をRigidbodyへ戻す
    targetRigidbody.velocity = velocity;
}

private void EnsureVisuals()
{
    // 既に生成済みなら二重生成しないようにする
    Transform existingVisual = transform.Find(VisualRootName);
    if (existingVisual != null)
    {
        visualRoot = existingVisual;
        return;
    }

    // 発光用と粒子用のマテリアルを作成する
    auraMaterial = CreateAuraMaterial();
    particleMaterial = CreateParticleMaterial();

    // 自動生成した見た目用オブジェクトをまとめる親を作る
    visualRoot = new GameObject(VisualRootName).transform;
    visualRoot.SetParent(transform, false);

    // モデル内の各メッシュに沿って発光と粒子を生成する
    int effectCount = CreateMeshEffects();
    if (effectCount == 0)
    {
        Debug.LogWarning($"{name}: WarpGate could not find a MeshFilter on this model, so only warp behavior is active.", this);
    }
}

private int CreateMeshEffects()
{
    int effectCount = 0;

    // 自分自身と子オブジェクトからエフェクトを沿わせるMeshFilterを探す
    MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
    for (int i = 0; i < meshFilters.Length; i++)
    {
        MeshFilter sourceFilter = meshFilters[i];

        // メッシュが無いものまたは自動生成済みエフェクト配下のものは対象外
        if (sourceFilter == null || sourceFilter.sharedMesh == null || sourceFilter.transform.IsChildOf(visualRoot))
        {
            continue;
        }

        // MeshRendererが無いメッシュは画面表示用ではないためエフェクト対象外
        MeshRenderer sourceRenderer = sourceFilter.GetComponent<MeshRenderer>();
        if (sourceRenderer == null)
        {
            continue;
        }

        // 元メッシュの形に沿う発光オーラと粒子を作る。
        CreateAura(sourceFilter, sourceRenderer);
        CreateMeshParticles(sourceFilter);
        effectCount++;
    }

    return effectCount;
}

private void CreateAura(MeshFilter sourceFilter, MeshRenderer sourceRenderer)
{
    // 元メッシュの子として、同じ形の発光用メッシュを追加
    GameObject auraObject = new GameObject(sourceFilter.name + AuraSuffix);
    auraObject.transform.SetParent(sourceFilter.transform, false);
    auraObject.transform.localPosition = Vector3.zero;
    auraObject.transform.localRotation = Quaternion.identity;
    auraObject.transform.localScale = Vector3.one * effectScaleOffset;

    // 元メッシュと同じMeshを使い少し大きく表示して輪郭発光のように見せる
    MeshFilter auraFilter = auraObject.AddComponent<MeshFilter>();
    MeshRenderer auraRenderer = auraObject.AddComponent<MeshRenderer>();
    auraFilter.sharedMesh = sourceFilter.sharedMesh;
    auraRenderer.sharedMaterial = auraMaterial;
    auraRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    auraRenderer.receiveShadows = false;
    auraRenderer.enabled = sourceRenderer.enabled;

    // Updateで脈動させるため元のScaleを保存。
    pulseVisuals.Add(new VisualPulse
    {
        Transform = auraObject.transform,
        BaseScale = auraObject.transform.localScale
    });
}

private void CreateMeshParticles(MeshFilter sourceFilter)
{
    // 元メッシュの子としてメッシュ表面から粒子を出すParticleSystemを追加する
    GameObject particlesObject = new GameObject(sourceFilter.name + ParticleSuffix);
    particlesObject.transform.SetParent(sourceFilter.transform, false);
    particlesObject.transform.localPosition = Vector3.zero;
    particlesObject.transform.localRotation = Quaternion.identity;
    particlesObject.transform.localScale = Vector3.one;

    // 粒子の寿命・速度・大きさ・色などの基本設定
    ParticleSystem particles = particlesObject.AddComponent<ParticleSystem>();
    ParticleSystem.MainModule main = particles.main;
    main.startColor = portalColor;
    main.startLifetime = 1.1f;
    main.startSpeed = 0.18f;
    main.startSize = particleSize;
    main.simulationSpace = ParticleSystemSimulationSpace.Local;

    // InspectorのparticleRateで1秒あたりの粒子量を調整できる
    ParticleSystem.EmissionModule emission = particles.emission;
    emission.rateOverTime = particleRate;

    // 粒子の発生源を元モデルのメッシュ表面にする
    ParticleSystem.ShapeModule shape = particles.shape;
    shape.shapeType = ParticleSystemShapeType.Mesh;
    shape.mesh = sourceFilter.sharedMesh;
    shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
    shape.normalOffset = 0.03f;

    // 粒子用マテリアルを設定する
    ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
    particleRenderer.material = particleMaterial;
}

private Material CreateAuraMaterial()
{
    // 元モデルの上に重ねる半透明で発光するマテリアルを作る
    Material material = new Material(Shader.Find("Standard"));
    Color auraColor = portalColor;
    auraColor.a = 0.42f;
    material.color = auraColor;
    material.EnableKeyword("_EMISSION");
    material.SetColor("_EmissionColor", portalColor * 2.4f);
    ConfigureTransparentMaterial(material);
    return material;
}

private Material CreateParticleMaterial()
{
    // 粒子用の発光マテリアルを作る。専用Shaderが無い場合はStandardに戻す
    Shader particleShader = Shader.Find("Particles/Standard Unlit");
    Material material = new Material(particleShader != null ? particleShader : Shader.Find("Standard"));
    material.color = portalColor;
    material.EnableKeyword("_EMISSION");
    material.SetColor("_EmissionColor", portalColor * 2f);
    ConfigureTransparentMaterial(material);
    return material;
}

private void ConfigureTransparentMaterial(Material material)
{
    // マテリアルを半透明描画にして発光エフェクトとして重ねられるようにする
    material.SetFloat("_Mode", 3f);
    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
    material.SetInt("_ZWrite", 0);
    material.DisableKeyword("_ALPHATEST_ON");
    material.EnableKeyword("_ALPHABLEND_ON");
    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    material.renderQueue = 3000;
}
}