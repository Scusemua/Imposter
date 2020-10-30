// Defines methods and properties common to all roles.
public interface IRole {
    // Name of the role.
    string Name { get; }

    float MovementSpeed { get; set; }

    float SprintDuration { get; set; }

    float SprintBoost { get; set; }

    // Cooldowns (e.g., kill frequency/interval).
    float PrimaryActionCooldown { get; set; }
    float SecondaryActionCooldown { get; set; }
    float TertiaryActionCooldown { get; set; }

    // The last times these actions were used (to determine if they are available again or not).
    float PrimaryActionLastUse { get; set; }
    float SecondaryActionLastUse { get; set; }
    float TertiaryActionLastUse { get; set; }

    // With standard crewmates, their primary action does nothing.
    // With sheriffs, their primary action is using their detective scanner.
    // With imposters, their primary action is killing.
    void PrimaryAction();

    void SecondaryAction();

    void TertiaryAction();
}