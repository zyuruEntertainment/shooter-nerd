using UnityEngine;

public class BotController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BotSettings settings;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Vision Eye (optional)")]
    [SerializeField] private Transform eyePoint;

    private BotVision vision;
    private BotHearing hearing;
    private BotPerception perception;
    private BotMovement movement;
    private ArcadeWeaponController weaponController;
    private BotJumpController jumpController;
    private BotDashController dashController;
    private PlayerDash playerDash;
    private BotStateMachine stateMachine;

    private CharacterController controller;
    private PlayerPhysics physics;
    private CombatHealth health;
    private PlayerSpriteController spriteController;
    private PlayerCrouch crouch;

    private float visionTimer;
    private float stateEvalTimer;
    private float wanderTimer;
    private Vector3 wanderTarget;
    private bool hasWanderTarget;

    private int patrolIndex;
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity; // untuk leading (prediksi gerakan pemain)

    private bool justDashed;
    private bool isCharging;
    private float chargeTimer;
    private float strafeTimer;
    private float chaseJumpTimer;
    private int strafeDirection = 1;
    private float strafeChangeInterval;

    // Crouch (jongkok) untuk membuat bot lebih sulit ditembak saat charge.
    private bool isCrouching;
    private float crouchTimer;
    private float crouchCooldownTimer;

    private float lastDamageTime = -999f;
    private float lastChargeTime = -999f;
    private float _lastShootDebugTime = -999f;

    // Reaksi: jeda sebelum bot merespons pemain yang baru terlihat.
    private float reactionTimer;
    private float requiredReactionDelay;
    private bool wasVisible;

    // === TRACKING DELAY (aim human-like) ===
    // Posisi aim yang diikuti bot (dengan lag/smoothing).
    private Vector3 trackedAimPosition;
    private bool hasTrackedAimPosition;
    // Timer reaksi sebelum bot mulai mengikuti pergerakan player.
    private float aimReactionTimer;
    // Timer untuk delay saat player berpindah arah tiba-tiba.
    private float aimDirectionChangeTimer;
    // Arah player sebelumnya (untuk deteksi perubahan arah mendadak).
    private Vector3 lastPlayerVelocity;
    private bool hasLastPlayerVelocity;

    public bool HasBeenDamagedRecently =>
        settings != null && Time.time - lastDamageTime <= settings.DamageMemoryDuration;

    public float LastChargeTime => lastChargeTime;

    public BotSettings Settings => settings;
    public BotPerception Perception => perception;
    public BotStateMachine StateMachine => stateMachine;
    public bool HasPatrolPoints => patrolPoints != null && patrolPoints.Length > 0;
    public bool JustDashed => justDashed;
    public bool IsCharging => isCharging;
    public void ClearJustDashed() { justDashed = false; }

    // Apakah reaksi sudah lewat (bisa berubah ke keadaan berkelahi).
    public bool CanReact => reactionTimer <= 0f;

    public float DistanceToPlayer
    {
        get
        {
            if (playerTarget == null) return float.MaxValue;
            return Vector3.Distance(transform.position, playerTarget.position);
        }
    }

    // Prediksi posisi pemain untuk pelarian/antisipasi.
    public Vector3 PredictedPlayerPosition(float leadTime)
    {
        if (playerTarget == null) return transform.position;
        Vector3 vel = playerVelocity;
        return playerTarget.position + vel * leadTime;
    }

    private void Awake()
    {
        if (settings == null) settings = GetComponent<BotSettings>();
        if (settings == null)
        {
            Debug.LogError("BotController: BotSettings missing.", this);
            enabled = false;
            return;
        }

        vision = GetComponent<BotVision>();
        hearing = GetComponent<BotHearing>();
        perception = GetComponent<BotPerception>();
        movement = GetComponent<BotMovement>();
        weaponController = GetComponent<ArcadeWeaponController>();
        jumpController = GetComponent<BotJumpController>();
        dashController = GetComponent<BotDashController>();
        playerDash = GetComponent<PlayerDash>();

        // Bot tidak boleh ikut input mouse global dari ArcadeWeaponController.
        if (weaponController != null)
            weaponController.AutoFireFromInput = false;
        stateMachine = GetComponent<BotStateMachine>();

        controller = GetComponent<CharacterController>();
        physics = GetComponent<PlayerPhysics>();
        health = GetComponent<CombatHealth>();
        spriteController = GetComponent<PlayerSpriteController>();
        crouch = GetComponent<PlayerCrouch>();

        // Bot mengontrol status jongkok secara terprogram (bukan input keyboard).
        if (crouch != null)
            crouch.IgnoreInput = true;

        if (stateMachine == null) stateMachine = gameObject.AddComponent<BotStateMachine>();

        if (vision != null)
        {
            if (eyePoint != null) vision.SetEye(eyePoint);
        }

        // Target musuh ditentukan berdasarkan tim, bukan tag Player.
        AcquireTarget();

        if (vision != null) vision.SetTarget(playerTarget);

        if (playerTarget != null)
            lastPlayerPosition = playerTarget.position;

        // Subscribe ke damage agar bot dapat pelarian saat ditembak.
        if (health != null)
            health.OnTakeDamage += OnBotTakeDamage;

        visionTimer = settings.VisionCheckInterval;
        stateEvalTimer = settings.StateEvaluationInterval;
        wanderTimer = 0f;
        strafeChangeInterval = Random.Range(settings.StrafeMinInterval, settings.StrafeMaxInterval);

        justDashed = false;
        isCharging = false;
        chargeTimer = 0f;
        strafeTimer = 0f;
        reactionTimer = 0f;
        requiredReactionDelay = 0f;
        wasVisible = false;
        isCrouching = false;
        crouchTimer = 0f;
        crouchCooldownTimer = 0f;

        // Inisialisasi tracking delay.
        hasTrackedAimPosition = false;
        trackedAimPosition = playerTarget != null ? playerTarget.position : transform.position;
        aimReactionTimer = 0f;
        aimDirectionChangeTimer = 0f;
        hasLastPlayerVelocity = false;
    }

    private void OnBotTakeDamage(float amount)
    {
        lastDamageTime = Time.time;

        // Cooldown charge: jika masih dalam cooldown, tidak charge lagi.
        if (settings != null && Time.time - lastChargeTime < settings.ChargeCooldown)
            return;

        // Saat ditembak, hentikan perilaku lalu CHARGE ke arah player (menyerang balik).
        isCharging = true;
        chargeTimer = settings.ChargeDuration;
        lastChargeTime = Time.time;
        justDashed = false; // izinkan dash pada evaluasi berikutnya
        isCrouching = false;
        crouchTimer = 0f;
        crouchCooldownTimer = 0f;
    }

    private void Start()
    {
        // BotStateMachine resolves its controller reference in its own Awake.
    }

    private void Update()
    {
        if (settings == null) return;

        float dt = Time.deltaTime;

        // Re-acquire target musuh jika target null/mati.
        CombatHealth targetHealth = playerTarget != null ? playerTarget.GetComponent<CombatHealth>() : null;
        if (playerTarget == null || (targetHealth != null && targetHealth.IsDead))
            AcquireTarget();
        if (vision != null && vision.TargetTransform != playerTarget)
            vision.SetTarget(playerTarget);

        // Track kecepatan pemain untuk prediksi (leading).
        TrackPlayerVelocity(dt);

        // Reaksi timer.
        if (reactionTimer > 0f)
            reactionTimer -= dt;

        // Deteksi transisi terlihat -> atur jeda reaksi acak.
        bool visibleNow = vision != null && vision.CanSeePlayer;
        if (visibleNow && !wasVisible)
        {
            requiredReactionDelay = Random.Range(settings.ReactionTimeMin, settings.ReactionTimeMax);
            reactionTimer = requiredReactionDelay;
        }
        wasVisible = visibleNow;

        // Vision check pada interval.
        visionTimer -= dt;
        if (visionTimer <= 0f && vision != null)
        {
            vision.CheckVision();
            visionTimer = settings.VisionCheckInterval;
        }

        // Perception refresh (murah).
        if (perception != null) perception.Refresh();

        // State evaluation pada interval.
        stateEvalTimer -= dt;
        if (stateEvalTimer <= 0f)
        {
            if (stateMachine != null) stateMachine.Evaluate();
            stateEvalTimer = settings.StateEvaluationInterval;
        }

        // Update tujuan gerakan berdasarkan state.
        UpdateMovementDestination();

        // Movement tick setiap frame.
        if (movement != null)
        {
            float speed = GetCurrentSpeed();
            bool canMove = CanMoveInCurrentState();
            BotState st = stateMachine != null ? stateMachine.CurrentState : BotState.Idle;
            // Saat Strafe / Attack / Charge: jangan memutar tubuh ke arah gerak (bot menghadap player).
            // Rotasi tangani oleh aim lock ke pemain.
            bool rotateTowardMovement = st != BotState.Strafe &&
                                        st != BotState.Attack &&
                                        st != BotState.Charge;

            // Rotasi lebih lincah saat Charge dan Strafe (mengejar/mengelilingi player).
            movement.RotationSpeedMultiplier = (st == BotState.Charge || st == BotState.Strafe)
                ? settings.AgileRotationMultiplier : 1f;

            movement.Tick(dt, speed, canMove, rotateTowardMovement);
        }

        // Jump / dash saat mengejar atau berkelahi atau pelarian.
        BotState curState = stateMachine != null ? stateMachine.CurrentState : BotState.Idle;
        bool chasing = curState == BotState.Chase || curState == BotState.Attack || curState == BotState.Strafe;
        bool charging = curState == BotState.Charge;
        bool combatOrCharge = chasing || charging;
        if (combatOrCharge && playerTarget != null)
        {
            float dist = DistanceToPlayer;
            Vector3 toPlayer = playerTarget.position - transform.position;

            if (jumpController != null)
            {
                bool clearance = jumpController.HasForwardClearance(transform.forward, 1.5f);
                jumpController.TryJump(chasing, dist, clearance);
            }

            if (dashController != null)
            {
                if (charging)
                    HandleChargeDash(toPlayer); // dash TERUS arah player saat charge
                else
                    HandleCombatDash(toPlayer, dist);
            }
        }

        // Lompat sesekali saat mengejar, dan lebih sering saat CHARGE (mendekat player sambil lompat).
        if ((curState == BotState.Chase || curState == BotState.Charge) && playerTarget != null && jumpController != null && physics.IsGrounded)
        {
            chaseJumpTimer -= dt;
            if (chaseJumpTimer <= 0f)
            {
                jumpController.TryChaseJump();
                // Lompat lebih sering saat charge agar lebih sulit ditangkap/ditembak.
                chaseJumpTimer = curState == BotState.Charge ? 1.5f : Random.Range(3f, 6f);
            }
        }

        // Crouch secara acak saat charge (membuat bot lebih sulit ditembak).
        UpdateCrouch(dt);

        // Track dash rising edge (no-op).

        // BotRangedCombat aktif saat chase/attack/strafe/CHARGE (menembak sambil mendekat player).
        // Chase juga menyala agar bot tetap menembak sambil mengejar player.
        if (weaponController != null)
            weaponController.enabled = curState == BotState.Chase || curState == BotState.Attack ||
                                       curState == BotState.Strafe || curState == BotState.Charge;

        // Bot menembak menggunakan ArcadeWeaponController (sama seperti player).
        // Hanya tembak jika dalam keadaan bertempur, player terlihat, dan punya senjata.
        bool inCombatState = curState == BotState.Attack || curState == BotState.Strafe ||
                             curState == BotState.Charge || curState == BotState.Chase;
        bool hasTarget = playerTarget != null;
        bool canSeePlayer = vision != null && vision.CanSeePlayer;
        bool hasWeapon = weaponController != null && weaponController.HasWeapon;
        bool hasAmmo = weaponController != null && weaponController.CurrentAmmo > 0;
        bool hasReserve = weaponController != null && weaponController.ReserveAmmo > 0;

        // AUTO-RELOAD: jika dalam combat, punya senjata, ammo habis, tapi masih ada reserve → reload otomatis.
        if (inCombatState && hasWeapon && !hasAmmo && hasReserve)
        {
            Debug.Log($"[BotController] {gameObject.name} >>> AUTO-RELOAD: ammo {weaponController.CurrentAmmo}/{weaponController.ReserveAmmo} → reload...");
            weaponController.Reload();
        }

        bool canShoot = inCombatState && hasTarget && canSeePlayer && hasWeapon && hasAmmo;

        if (canShoot)
        {
            weaponController.Fire();
        }
        else
        {
            // Log hanya sekali setiap 2 detik agar tidak spam console.
            if (Time.time - _lastShootDebugTime > 2f)
            {
                _lastShootDebugTime = Time.time;
                string reasons = "";
                if (!inCombatState) reasons += "state tidak combat (" + curState + "), ";
                if (!hasTarget) reasons += "no target, ";
                if (!canSeePlayer) reasons += "tidak lihat player, ";
                if (!hasWeapon) reasons += "tidak punya senjata, ";
                if (!hasAmmo) reasons += "ammo habis, ";
                Debug.LogWarning($"[BotController] {gameObject.name} tidak menembak: {reasons}");
            }
        }

        // Feedback animasi tembakan pada sprite.
        bool isShooting = canShoot;

        if (spriteController != null)
        {
            spriteController.SetShootingState(isShooting);
            spriteController.SetCrouchState(isCrouching); // sinkronkan animasi jongkok ke sprite
        }

        // === AIM LOCK KE PLAYER DENGAN TRACKING DELAY (human-like) ===
        // Bot tidak langsung menghadap player setiap frame, tapi menggunakan
        // posisi aim yang diikuti dengan smoothing dan reaction delay.
        if ((curState == BotState.Chase || curState == BotState.Attack ||
             curState == BotState.Strafe || curState == BotState.Charge) &&
            playerTarget != null)
        {
            UpdateTrackedAimPosition(dt, curState);
            ApplyTrackedAimRotation(dt, curState);
        }
    }

    // === TRACKING DELAY METHODS ===

    /// <summary>
    /// Update posisi aim yang diikuti bot dengan smoothing dan reaction delay.
    /// Posisi aim bergerak bertahap menuju posisi player, tidak langsung.
    /// </summary>
    private void UpdateTrackedAimPosition(float dt, BotState curState)
    {
        if (playerTarget == null) return;

        float reactionDelay = settings != null ? settings.AimReactionDelay : 0.15f;
        float aimSmoothing = settings != null ? settings.AimSmoothing : 0.5f;
        float followSpeed = settings != null ? settings.AimPositionFollowSpeed : 8f;

        // Deteksi perubahan arah player secara mendadak.
        Vector3 currentVel = playerVelocity;
        bool directionChanged = false;
        if (hasLastPlayerVelocity)
        {
            float velDelta = Vector3.Distance(currentVel, lastPlayerVelocity);
            if (velDelta > 3f) // player berpindah arah dengan cepat
            {
                directionChanged = true;
            }
        }
        lastPlayerVelocity = currentVel;
        hasLastPlayerVelocity = true;

        // Saat player berpindah arah mendadak, tambahkan delay sebelum bot mengikuti.
        if (directionChanged && !hasTrackedAimPosition == false)
        {
            aimDirectionChangeTimer = reactionDelay * 0.5f;
        }

        // Kurangi timer delay.
        if (aimDirectionChangeTimer > 0f)
        {
            aimDirectionChangeTimer -= dt;
        }

        // Reaksi delay: bot tidak langsung mulai mengikuti pergerakan player.
        if (!hasTrackedAimPosition)
        {
            trackedAimPosition = playerTarget.position;
            hasTrackedAimPosition = true;
            aimReactionTimer = reactionDelay;
        }

        // Saat player baru terlihat atau baru bergerak, tambahkan reaksi delay.
        if (aimReactionTimer > 0f)
        {
            aimReactionTimer -= dt;
            // Selama reaksi delay, posisi aim tetap di posisi sebelumnya (tidak mengikuti).
            return;
        }

        // Jika dalam delay perubahan arah, posisi aim tetap (tidak mengikuti).
        if (aimDirectionChangeTimer > 0f)
            return;

        // Smooth follow: posisi aim bergerak bertahap menuju posisi player.
        // Gunakan Lerp dengan smoothing factor untuk efek lag.
        Vector3 targetPos = playerTarget.position;
        float t = 1f - Mathf.Pow(aimSmoothing, dt * followSpeed);
        trackedAimPosition = Vector3.Lerp(trackedAimPosition, targetPos, t);
    }

    /// <summary>
    /// Terapkan rotasi bot berdasarkan posisi aim yang diikuti (dengan smoothing).
    /// </summary>
    private void ApplyTrackedAimRotation(float dt, BotState curState)
    {
        if (playerTarget == null) return;

        Vector3 toTracked = trackedAimPosition - transform.position;
        toTracked.y = 0f;
        if (toTracked.sqrMagnitude < 0.0001f) return;

        float trackSpeed = settings != null ? settings.AimTrackingSpeed : 360f;
        float rotSpeed = trackSpeed * (curState == BotState.Charge || curState == BotState.Strafe
            ? (settings != null ? settings.AgileRotationMultiplier : 1.6f) : 1f);

        // Smooth rotation: gunakan RotateTowards dengan kecepatan terbatas.
        Quaternion targetRot = Quaternion.LookRotation(toTracked.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRot, rotSpeed * dt);
    }

    private void AcquireTarget()
    {
        if (health == null) return;

        Team selfTeam = health.Team;
        if (selfTeam == Team.None) selfTeam = Team.TeamB;

        CombatHealth enemy = TeamRegistry.FindNearestEnemy(transform.position, selfTeam);
        playerTarget = enemy != null ? enemy.transform : null;

        if (vision != null) vision.SetTarget(playerTarget);
        if (playerTarget != null) lastPlayerPosition = playerTarget.position;
    }

    private void TrackPlayerVelocity(float dt)
    {
        if (playerTarget == null) return;

        var pp = playerTarget.GetComponent<PlayerPhysics>();
        if (pp != null)
        {
            playerVelocity = pp.HorizontalVelocity;
            lastPlayerPosition = playerTarget.position;
            return;
        }

        // Fallback estimasi kecepatan dari selisih posisi.
        Vector3 cur = playerTarget.position;
        playerVelocity = (cur - lastPlayerPosition) / Mathf.Max(dt, 0.0001f);
        lastPlayerPosition = cur;
    }

    private void UpdateMovementDestination()
    {
        if (movement == null || stateMachine == null) return;

        BotState st = stateMachine.CurrentState;
        switch (st)
        {
            case BotState.Idle:
                movement.Stop();
                HandleWander();
                break;
            case BotState.Patrol:
                HandlePatrol();
                break;
            case BotState.Investigate:
                if (perception != null && perception.HasTarget)
                    movement.SetDestination(perception.TargetPosition);
                break;
            case BotState.Chase:
                // Langsung mendekati pemain. ArcadeWeaponController akan menembak jika sudah dalam attackRange.
                if (playerTarget != null)
                    movement.SetDestination(playerTarget.position);
                break;
            case BotState.Attack:
            case BotState.Strafe:
                HandleStrafe();
                break;
            case BotState.Search:
                if (vision != null && vision.HasLastKnown)
                    movement.SetDestination(vision.LastKnownPosition);
                else
                    movement.Stop();
                break;
            case BotState.Charge:
                HandleCharge();
                break;
            case BotState.Recover:
                movement.Stop();
                break;
        }
    }

    private void HandlePatrol()
    {
        if (HasPatrolPoints)
        {
            if (patrolPoints[patrolIndex] != null)
                movement.SetDestination(patrolPoints[patrolIndex].position);
            if (movement != null && Vector3.Distance(transform.position, patrolPoints[patrolIndex].position) < 1f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            }
        }
        else
        {
            HandleWander();
        }
    }

    private void HandleWander()
    {
        if (!hasWanderTarget || wanderTimer <= 0f)
        {
            PickWanderTarget();
            wanderTimer = settings.WanderInterval;
        }
        wanderTimer -= Time.deltaTime;
        if (hasWanderTarget)
            movement.SetDestination(wanderTarget);
    }

    private void PickWanderTarget()
    {
        float r = settings.WanderRadius;
        Vector2 rnd = Random.insideUnitCircle * r;
        wanderTarget = transform.position + new Vector3(rnd.x, 0f, rnd.y);
        hasWanderTarget = true;
    }

    /// <summary>
    /// Strafing pintar: lingkari pemain pada EngageRange, arah berubah secara acak,
    /// mundur jika terlalu dekat / mendekat jika terlalu jauh, dan kadang dash melingkar.
    /// </summary>
    private void HandleStrafe()
    {
        if (playerTarget == null || movement == null) return;

        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            strafeDirection = Random.value > 0.5f ? 1 : -1;
            strafeTimer = Random.Range(settings.StrafeMinInterval, settings.StrafeMaxInterval);
        }

        Vector3 toPlayer = playerTarget.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;
        Vector3 toPlayerDir = dist > 0.0001f ? toPlayer / dist : Vector3.forward;

        // Arah strafing: tegak lurus terhadap pemain (kiri/kanan).
        Vector3 strafeDir = Vector3.Cross(Vector3.up, toPlayerDir) * strafeDirection;

        // Pertahankan jarak dekat EngageRange.
        float engageRange = settings.EngageRange;
        Vector3 adjust = Vector3.zero;
        if (dist < engageRange * 0.5f)
            adjust -= toPlayerDir; // terlalu dekat, mundur
        else if (dist > engageRange)
            adjust += toPlayerDir; // terlalu jauh, mendekat sedikit

        Vector3 finalDir = (strafeDir + adjust);
        if (finalDir.sqrMagnitude < 0.0001f)
            finalDir = toPlayerDir;
        else
            finalDir.Normalize();

        // Tujuan di depan sejumlah jarak agar CharacterController memiliki tujuan gerak.
        movement.SetDestination(transform.position + finalDir * 8f);
    }

    /// <summary>
    /// Charge: berlari MENUJU player (menyerang balik) saat baru terkena damage.
    /// Bot lebih lincah: mencoba mendekat cepat, kadang jongkok agar sulit ditembak,
    /// dan terus menghadap player (aim-lock + tembak).
    /// </summary>
    private void HandleCharge()
    {
        if (playerTarget == null || movement == null) return;

        chargeTimer -= Time.deltaTime;
        if (chargeTimer <= 0f)
        {
            isCharging = false;
            return;
        }

        // Gerak lurus menuju pemain (menerjang), tanpa zig-zag menjauh.
        Vector3 toPlayer = playerTarget.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) toPlayer = transform.forward;
        toPlayer.Normalize();

        // Tujuan sedikit di depan pemain agar CharacterController punya arah gerak.
        movement.SetDestination(transform.position + toPlayer * 10f);
    }

    /// <summary>
    /// Atur status jongkok secara acak saat charge (sulit ditembak).
    /// Terhubung ke PlayerCrouch (collider mengecil) dan PlayerSpriteController (animasi).
    /// </summary>
    private void UpdateCrouch(float dt)
    {
        if (settings == null || crouch == null || spriteController == null) return;

        // Cooldown antar jongkok.
        if (crouchCooldownTimer > 0f)
            crouchCooldownTimer -= dt;

        if (isCharging && crouchCooldownTimer <= 0f && Random.value < settings.CrouchChance * dt)
        {
            // Mulai jongkok.
            isCrouching = true;
            crouchTimer = settings.CrouchDuration;
            crouchCooldownTimer = settings.CrouchCooldown;
            crouch.SetCrouching(true);
        }
        else if (isCrouching)
        {
            crouchTimer -= dt;
            if (crouchTimer <= 0f)
            {
                isCrouching = false;
                crouch.SetCrouching(false);
            }
        }

        // Pastikan sprite controller selalu sinkron.
        spriteController.SetCrouchState(isCrouching);
    }

    /// <summary>
    /// Dash kompatibel saat bertempur: kadang meluncur ke samping untuk mengelak peluru pemain.
    /// </summary>
    private void HandleCombatDash(Vector3 toPlayer, float dist)
    {
        if (dashController == null || physics == null || !physics.IsGrounded) return;
        if (playerDash != null && playerDash.IsDashing) return;

        Vector3 dir = toPlayer;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        else dir.Normalize();

        Vector3 sideways = Vector3.Cross(Vector3.up, dir) * strafeDirection;

        // Terlalu dekat: kadang dash mundur untuk menciptakan ruang.
        if (dist < settings.CloseRangeThreshold && Random.value < 0.03f)
        {
            dashController.TryDash(-dir, true, forceDash: true);
        }
        // Sesekali dash ke samping saat bertempur agar sulit dikejar.
        else if (dist < 8f && Random.value < 0.04f)
        {
            dashController.TryDash(sideways, true, forceDash: true);
        }
    }

    /// <summary>
    /// Dash charge paksa MENUJU pemain (forceDash bypass timer dekat-jauh).
    /// Membuat bot tiba-tiba mendekat dan sulit dielak.
    /// </summary>
    private void HandleChargeDash(Vector3 toPlayer)
    {
        if (dashController == null || physics == null || !physics.IsGrounded) return;
        if (playerDash != null && playerDash.IsDashing) return;

        Vector3 dir = toPlayer;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dashController.TryDash(dir.normalized, true, forceDash: true);
    }

    private float GetCurrentSpeed()
    {
        if (settings == null || stateMachine == null) return 0f;
        switch (stateMachine.CurrentState)
        {
            case BotState.Chase:
            case BotState.Attack:
                return settings.ChaseSpeed;
            case BotState.Strafe:
                return settings.StrafeSpeed;
            case BotState.Charge:
                return settings.ChargeSpeed;
            case BotState.Patrol:
                return settings.PatrolSpeed;
            case BotState.Investigate:
            case BotState.Search:
                return settings.WalkSpeed;
            case BotState.Idle:
                return settings.WalkSpeed * 0.4f;
            default:
                return 0f;
        }
    }

    private bool CanMoveInCurrentState()
    {
        if (stateMachine == null) return true;
        switch (stateMachine.CurrentState)
        {
            case BotState.Recover:
                return false;
            default:
                return true;
        }
    }
}
