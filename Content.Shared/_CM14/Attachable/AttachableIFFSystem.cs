﻿using Content.Shared._CM14.Attachable.Components;
using Content.Shared._CM14.Attachable.Events;
using Content.Shared._CM14.Weapons.Ranged.IFF;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._CM14.Attachable;

public sealed class AttachableIFFSystem : EntitySystem
{
    [Dependency] private readonly AttachableHolderSystem _holder = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableIFFComponent, AttachableAlteredEvent>(OnAttachableIFFAltered);
        SubscribeLocalEvent<AttachableIFFComponent, AttachableGrantIFFEvent>(OnAttachableIFFGrant);

        SubscribeLocalEvent<GunAttachableIFFComponent, AmmoShotEvent>(OnGunAttachableIFFAmmoShot);
    }

    private void OnAttachableIFFAltered(Entity<AttachableIFFComponent> ent, ref AttachableAlteredEvent args)
    {
        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                UpdateGunIFF(args.Holder);
                break;
            case AttachableAlteredType.Detached:
                UpdateGunIFF(args.Holder);
                break;
        }
    }

    private void OnAttachableIFFGrant(Entity<AttachableIFFComponent> ent, ref AttachableGrantIFFEvent args)
    {
        args.Grants = true;
    }

    private void OnGunAttachableIFFAmmoShot(Entity<GunAttachableIFFComponent> ent, ref AmmoShotEvent args)
    {
        _gunIFF.GiveAmmoIFF(ent, ref args);
    }

    private void UpdateGunIFF(EntityUid gun)
    {
        if (!TryComp(gun, out AttachableHolderComponent? holder))
            return;

        var ev = new AttachableGrantIFFEvent();
        _holder.RelayEvent((gun, holder), ref ev);

        if (ev.Grants)
            EnsureComp<GunAttachableIFFComponent>(gun);
        else
            RemCompDeferred<GunAttachableIFFComponent>(gun);
    }
}
