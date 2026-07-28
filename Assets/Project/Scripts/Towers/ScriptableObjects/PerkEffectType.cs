namespace Towers.ScriptableObjects
{
    /// <summary>
    /// Що саме робить картка прокачки. Значення читає PerkManager і застосовує
    /// відповідний ефект, а числа бере з PerkConfig.Amount / SecondaryAmount.
    /// </summary>
    public enum PerkEffectType
    {
        /// <summary>+Amount дамагу до всіх ПВО (і вже збудованих, і майбутніх).</summary>
        AntiAirDamage,

        /// <summary>+Amount (частка, 0.5 = +50%) до дальності звичайних турелей.</summary>
        GroundTurretRange,

        /// <summary>Авторемонт усіх будівель, крім фортеці: +Amount HP кожні SecondaryAmount секунд.</summary>
        AutoRepairBuildings,

        /// <summary>Авторемонт фортеці: +Amount HP кожні SecondaryAmount секунд.</summary>
        AutoRepairFortress,

        /// <summary>"Німцеріз": раз на SecondaryAmount секунд б'є по випадковому ворогу, ніби пальцем.</summary>
        AutoFingerStrike,

        /// <summary>"Прибрати колаборанта": разово дає Amount пончиків.</summary>
        InstantPonchics,
    }
}
