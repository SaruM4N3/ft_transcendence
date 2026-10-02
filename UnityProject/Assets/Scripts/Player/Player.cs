using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Root player component: movement/actions/stats/customization/feedback/pig-form, split by concern across Player.*.cs files.
public partial class Player : NetworkBehaviour, IDamageable
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private PlayerInput playerInput;
    private SortingLayer_Auto sortingLayerAuto;

    private bool IsBlocked => PauseManager.IsPaused;

    // False for stripped instances (e.g. the preview doll) - also checked in OnNetworkSpawn/Despawn, which ignore enabled.
    private bool isValidInstance;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        isValidInstance = GetComponent<NetworkObject>() != null && rb != null;
        if (!isValidInstance)
        {
            enabled = false;
            return;
        }

        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        playerInput = GetComponent<PlayerInput>();
        sortingLayerAuto = GetComponent<SortingLayer_Auto>();

        AwakeStats();
        AwakeCustomization();
        AwakeFeedback();
        AwakePigForm();
        AwakeActions();
    }

    void OnEnable()
    {
        OnEnableFeedback();
    }

    void OnDisable()
    {
        OnDisableFeedback();
    }

    void Start()
    {
        StartMovement();
        StartStats();
        StartCustomization();
        StartPigForm();
    }

    public override void OnNetworkSpawn()
    {
        if (!isValidInstance)
            return;

        SpawnMovement();
        SpawnStats();
        SpawnCustomization();
    }

    public override void OnNetworkDespawn()
    {
        if (!isValidInstance)
            return;

        DespawnMovement();
        DespawnStats();
        DespawnCustomization();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        OnDestroyFeedback();
    }

    void Update()
    {
        UpdateMovement();
        UpdateMountCollisionIgnore();
        UpdateMount();
        UpdateMountInteractVisibility();
        UpdateStats();
        UpdatePigForm();
        UpdateActions();
    }

    void LateUpdate()
    {
        LateUpdateMount();
        LateUpdateMountSorting();
    }
}
