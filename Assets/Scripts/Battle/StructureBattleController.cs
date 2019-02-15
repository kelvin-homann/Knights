using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StructureBattleController : BattleController, IAttackable, IDestructable
{
    // structure attributes
    [Header("Structure Attributes")]
    
    public float height = 2f;
    public Renderer healthRenderer;
    private MaterialPropertyBlock healthRendererMaterialPropertyBlock;
    private int healthRendererMaterialColorId;
    public Canvas healthCanvas;
    public Slider healthSlider;
    public Image healthFillImage;
    public Gradient healthColorGradient;

    private Castle castleController;

    private void Start()
    {
        castleController = GetComponent<Castle>();
        InitializeHealthIndicator();
    }

    private void Awake()
    {
        BattleManager.GetInstance().RegisterStructureBattleController(this);
        UpdateHealthIndicator();
    }

    private void OnDestroy()
    {
        BattleManager.GetInstance().UnregisterStructureBattleController(this);
    }

    private void FixedUpdate()
    {
        // update relative health points of castle controller to trigger damage animations
        castleController.healthPointsRelative = currentHealthPoints / maxHealthPoints;
        UpdateHealthIndicator();
    }

    public void OnAttack(Attack attackData)
    {
        if(attackData == null)
            return;

        /* don't exert damage if the attacker was destroyed; an attacker may be destroyed after it transmitted an attack (delayed) 
           but before the attack was received here */
        if(attackData.Attacker != null && attackData.Attacker.destroyed)
            return;

        if(attackData.AttackResult == EAttackResult.Succeeded_Damaged || attackData.AttackResult == EAttackResult.Succeeded_Destroyed ||
            attackData.AttackResult == EAttackResult.Pending)
        {
            float newHealthPoints = Mathf.Max(currentHealthPoints - attackData.DamagePoints, 0f);
            bool isLethal = newHealthPoints == 0f;

            string attackedWithText = attackData.AttackType == EAttackType.Melee ? "hand weapon" : "projectile";
            if(attackData.AttackDefinition.attackedWithText.Length > 0)
                attackedWithText = attackData.AttackDefinition.attackedWithText;

            PlayHitSound(attackData.AttackDefinition);

            if(isLethal)
            {
                OnDestruct(attackData);
            }
            else
            {
                currentHealthPoints = newHealthPoints;

#if DEBUG
                LogSystem.Log(ELogMessageType.StructureBattleControllerDamaging, "{0} was attacked by <color=white>{1}</color> with <color=yellow>{2}</color> and received {3:0.00} damage points",
                    name, attackData.Attacker.name, attackedWithText, attackData.DamagePoints);
#endif

                attackData.AttackResult = EAttackResult.Succeeded_Damaged;
                if(attackData.AttackExecutedCallback != null)
                    attackData.AttackExecutedCallback(attackData);
            }

            UpdateHealthIndicator();
        }
    }

    public void OnDestruct(Attack attackData)
    {
        // only do this if there is a valid attack and if the attacker still exists
        if(attackData != null && attackData.Attacker != null && attackData.Attacker.gameObject != null)
        {
            CharacterBattleController attacker = attackData.Attacker;
            attackData.Attacker.attackTarget = null;

            string attackedWithText = attackData.AttackType == EAttackType.Melee ? "hand weapon" : "projectile";
            if(attackData.AttackDefinition.attackedWithText.Length > 0)
                attackedWithText = attackData.AttackDefinition.attackedWithText;

#if DEBUG
            LogSystem.Log(ELogMessageType.StructureBattleControllerDestroying, "{0} was destroyed by <color=white>{1}</color> with <color=yellow>{2}</color>",
                name, attacker.name, attackedWithText);
#endif

            attackData.AttackResult = EAttackResult.Succeeded_Destroyed;
            if(attackData.AttackExecutedCallback != null)
                attackData.AttackExecutedCallback(attackData);
        }
        else
        {
#if DEBUG
            LogSystem.Log(ELogMessageType.StructureBattleControllerDestroying, "{0} was destroyed", name);
#endif
        }

        BattleManager.GetInstance().UnregisterStructureBattleController(this);

        currentHealthPoints = 0f;
        // destruction handling
        //Destroy(gameObject, 3f);
        destroyed = true;
    }

    private void InitializeHealthIndicator()
    {
        if(healthRenderer == null)
        {
            Transform healthRendererTransform = transform.FindChildWithTag("Health Renderer");
            if(healthRendererTransform != null)
                healthRenderer = healthRendererTransform.GetComponent<Renderer>();
        }

        if(healthRenderer != null && healthRendererMaterialPropertyBlock == null)
        {
            healthRendererMaterialPropertyBlock = new MaterialPropertyBlock();
            healthRendererMaterialColorId = Shader.PropertyToID("_Color");
        }
    }

    private void UpdateHealthIndicator()
    {
        if(healthCanvas == null && healthRenderer == null)
            return;

        // enable or disable health canvas and/or renderer based on current display settings
        if(healthCanvas != null && healthCanvas.enabled != DisplaySettings.renderHealthBars)
            healthCanvas.enabled = DisplaySettings.renderHealthBars;
        if(healthRenderer != null && healthRenderer.enabled != DisplaySettings.renderHealthBars)
            healthRenderer.enabled = DisplaySettings.renderHealthBars;

        Color healthColor;
        float currentHealthRelative = currentHealthPoints / maxHealthPoints;

        // update health slider position
        if(healthSlider != null)
        {
            if(healthFillImage.fillMethod == Image.FillMethod.Radial360)
                healthSlider.value = 100f;
            else
                healthSlider.value = currentHealthRelative * 100f;
        }

        // use color gradient and evaluate momentary color value
        if(healthColorGradient != null)
        {
            healthColor = healthColorGradient.Evaluate(currentHealthRelative);
        }
        // fallback method
        else
        {
            if(currentHealthRelative < 0.5f)
                healthColor = Color.Lerp(Color.red, Color.yellow, currentHealthRelative * 2f);
            else
                healthColor = Color.Lerp(Color.yellow, Color.green, (currentHealthRelative - 0.5f) * 2f);
        }

        // set new health color to health renderer mesh material
        if(healthRenderer != null && healthRendererMaterialPropertyBlock != null)
        {
            healthRendererMaterialPropertyBlock.SetColor(healthRendererMaterialColorId, healthColor);
            healthRenderer.SetPropertyBlock(healthRendererMaterialPropertyBlock);
        }

        // set new health color to UI slider fill image
        if(healthCanvas != null && healthCanvas.enabled && healthFillImage != null)
            healthFillImage.color = healthColor;
    }

    private void PlayHitSound(AttackDefinition attackDefinition)
    {
        if(attackDefinition == null || attackDefinition.hitSound == null)
            return;
        AudioSource.PlayClipAtPoint(attackDefinition.hitSound.GetRandomAudioClip(), transform.position, 0.2f);
    }
}
