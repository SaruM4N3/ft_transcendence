<script lang="ts">
    let { data } = $props();
    let displayName = $state(data.user?.display_name ?? '');
    let color = $state(data.user?.color ?? 'Black');
    let playerClass = $state(data.user?.class ?? 'Warrior');
    let avatarUrl = $state<string | null>(data.user?.avatar_url ?? null);
    let error = $state('');
    let notice = $state('');
    let saving = $state(false);

    const colors = [
        { name: 'Blue', value: '#3985f6' },
        { name: 'Red', value: '#ec5a61' },
        { name: 'Yellow', value: '#e7bd48' },
        { name: 'Purple', value: '#9667e8' },
        { name: 'Black', value: '#171923' }
    ];
    const classes = ['Warrior', 'Archer', 'Lancer', 'Monk', 'Pawn'];

    function chooseAvatar(event: Event) {
        const input = event.currentTarget as HTMLInputElement;
        const file = input.files?.[0];
        error = '';
        if (!file) return;
        if (!['image/png', 'image/jpeg', 'image/webp', 'image/gif'].includes(file.type) || file.size > 500 * 1024) {
            error = 'Choose a PNG, JPG, WebP or GIF smaller than 500 KB.';
            input.value = '';
            return;
        }
        const reader = new FileReader();
        reader.onload = () => { avatarUrl = typeof reader.result === 'string' ? reader.result : null; };
        reader.onerror = () => { error = 'Could not read that image. Please try another.'; };
        reader.readAsDataURL(file);
    }

    async function saveSettings(event: SubmitEvent) {
        event.preventDefault();
        error = '';
        notice = '';
        saving = true;
        try {
            const response = await fetch('/api/settings', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ display_name: displayName, color, class: playerClass, avatar_url: avatarUrl })
            });
            const result = await response.json();
            if (!response.ok) throw new Error(result.error ?? 'Could not save your settings.');
            data.user = result.user;
            displayName = result.user.display_name;
            notice = 'Your player profile has been saved.';
        } catch (cause) {
            error = cause instanceof Error ? cause.message : 'Could not save your settings. Please try again.';
        } finally {
            saving = false;
        }
    }
</script>

<svelte:head><title>Player settings | Transcendence</title></svelte:head>

<section class="settings-page">
    <p class="eyebrow">PLAYER PROFILE</p>
    <h1>Your settings</h1>
    <p class="intro">Make your player profile yours. Your name, look and class appear wherever you play.</p>

    {#if data.user}
        <div class="editor-layout">
            <aside class="preview-panel" aria-label="Profile preview">
                <p class="section-kicker">LIVE PREVIEW</p>
                <div class="avatar" style={`--player-color: ${colors.find((item) => item.name === color)?.value ?? '#171923'}`}>
                    {#if avatarUrl}<img src={avatarUrl} alt="" />{:else}{displayName.slice(0, 1).toUpperCase() || '?'}{/if}
                </div>
                <h2>{displayName || 'Player name'}</h2>
                <div class="badges">
                    <span class="badge"><span class="badge-dot" style={`--player-color: ${colors.find((item) => item.name === color)?.value ?? '#171923'}`}></span>{color}</span>
                    <span class="badge">{playerClass}</span>
                </div>
                <p class="preview-copy">This is how your profile will look in game.</p>
            </aside>

            <form class="settings-form" onsubmit={saveSettings}>
                <section class="form-section">
                    <div class="section-heading"><span class="step">01</span><div><h2>Profile details</h2><p>Choose how other players see you.</p></div></div>
                    <label class="field-label" for="display-name">PLAYER NAME</label>
                    <input id="display-name" class="text-input" bind:value={displayName} minlength="2" maxlength="24" autocomplete="nickname" required />
                    <small>2–24 characters. Letters, numbers, spaces, _ and -.</small>
                </section>

                <section class="form-section">
                    <div class="section-heading"><span class="step">02</span><div><h2>Player color</h2><p>Pick an accent color for your profile.</p></div></div>
                    <div class="color-options" role="radiogroup" aria-label="Player color">
                        {#each colors as option}
                            <button type="button" class="color-choice" class:selected={color === option.name} style={`--swatch: ${option.value}`} aria-label={option.name} aria-pressed={color === option.name} onclick={() => color = option.name}></button>
                        {/each}
                    </div>
                    <span class="selected-color">Selected: {color}</span>
                </section>

                <section class="form-section">
                    <div class="section-heading"><span class="step">03</span><div><h2>Player class</h2><p>Choose the role that fits your style.</p></div></div>
                    <label class="field-label" for="player-class">CLASS</label>
                    <select id="player-class" class="text-input" bind:value={playerClass}>
                        {#each classes as option}<option value={option}>{option}</option>{/each}
                    </select>
                </section>

                <section class="form-section avatar-section">
                    <div class="section-heading"><span class="step">04</span><div><h2>Profile picture</h2><p>Add a picture to personalize your player card.</p></div></div>
                    <label class="upload-button" for="avatar-upload">{avatarUrl ? 'Choose another picture' : 'Upload a picture'}</label>
                    <input id="avatar-upload" class="visually-hidden" type="file" accept="image/png,image/jpeg,image/webp,image/gif" onchange={chooseAvatar} />
                    {#if avatarUrl}<button type="button" class="remove-avatar" onclick={() => avatarUrl = null}>Remove picture</button>{/if}
                    <small>PNG, JPG, WebP or GIF. Maximum file size 500 KB.</small>
                </section>

                {#if error}<p class="message error" role="alert">{error}</p>{/if}
                {#if notice}<p class="message success" role="status">{notice}</p>{/if}
                <div class="form-footer"><span>Your changes are saved to your account.</span><button class="save-button" type="submit" disabled={saving}>{saving ? 'Saving…' : 'Save changes'} <span aria-hidden="true">↗</span></button></div>
            </form>
        </div>
    {:else}
        <div class="empty-card"><h2>Sign in to personalize your profile</h2><p>Your player name, color, class and picture will appear here.</p><a class="save-button" href="/login">Log in</a></div>
    {/if}
</section>

<style>
    .settings-page { max-width: 1040px; margin: 0 auto; }
    .eyebrow, .section-kicker { margin: 0 0 .65rem; color: #f5b85c; font-size: .72rem; font-weight: 800; letter-spacing: .15em; }
    h1 { margin: 0; font-size: clamp(2.4rem, 7vw, 3.7rem); letter-spacing: -.06em; }
    .intro { margin: .8rem 0 2rem; color: #aeb8d3; line-height: 1.6; }
    .editor-layout { display: grid; grid-template-columns: minmax(230px, .72fr) minmax(0, 1.5fr); align-items: start; gap: 1.2rem; }
    .preview-panel, .settings-form, .empty-card { border: 1px solid rgba(189,204,242,.13); border-radius: 1.2rem; background: linear-gradient(145deg, rgba(29,37,65,.96), rgba(19,24,43,.96)); box-shadow: 0 24px 80px rgba(0,0,0,.18); }
    .preview-panel { position: sticky; top: 1rem; padding: 1.5rem; text-align: center; }
    .section-kicker { color: #8592b2; font-size: .65rem; }
    .avatar { display: grid; width: 6rem; height: 6rem; margin: 1.35rem auto 1rem; place-items: center; overflow: hidden; border: 1px solid rgba(255,255,255,.3); border-radius: 1.45rem; color: white; background: var(--player-color); font-size: 2.2rem; font-weight: 850; box-shadow: 0 0 2rem color-mix(in srgb, var(--player-color) 42%, transparent); }
    .avatar img { width: 100%; height: 100%; object-fit: cover; }
    .preview-panel h2 { margin: 0 0 .85rem; overflow-wrap: anywhere; font-size: 1.4rem; }
    .badges { display: flex; flex-wrap: wrap; justify-content: center; gap: .5rem; }
    .badge { display: inline-flex; align-items: center; gap: .45rem; padding: .42rem .65rem; border: 1px solid rgba(189,204,242,.14); border-radius: 999px; color: #d6def3; background: rgba(255,255,255,.045); font-size: .76rem; }
    .badge-dot { width: .58rem; height: .58rem; border: 1px solid rgba(255,255,255,.35); border-radius: 50%; background: var(--player-color); }
    .preview-copy { margin: 1.2rem 0 0; color: #7e8aa8; font-size: .8rem; line-height: 1.5; }
    .settings-form { padding: clamp(1.2rem, 4vw, 2rem); }
    .form-section { padding: 0 0 1.5rem; margin: 0 0 1.5rem; border-bottom: 1px solid rgba(189,204,242,.1); }
    .section-heading { display: flex; align-items: flex-start; gap: .8rem; margin-bottom: 1.15rem; }
    .step { display: grid; width: 2rem; height: 2rem; flex: 0 0 auto; place-items: center; border: 1px solid rgba(245,184,92,.25); border-radius: .65rem; color: #f5b85c; font-size: .7rem; font-weight: 800; }
    .section-heading h2 { margin: 0; font-size: 1.05rem; }
    .section-heading p { margin: .25rem 0 0; color: #8996b4; font-size: .83rem; }
    .field-label { display: block; margin-bottom: .45rem; color: #aeb8d3; font-size: .68rem; font-weight: 800; letter-spacing: .1em; }
    .text-input { width: 100%; box-sizing: border-box; padding: .82rem .9rem; border: 1px solid rgba(189,204,242,.18); border-radius: .65rem; outline: none; color: #f1f4ff; background: rgba(8,12,25,.45); font: inherit; }
    .text-input:focus { border-color: #f5b85c; box-shadow: 0 0 0 3px rgba(245,184,92,.12); }
    select.text-input { color-scheme: dark; }
    .form-section small { display: block; margin-top: .5rem; color: #7e8aa8; font-size: .75rem; }
    .color-options { display: flex; flex-wrap: wrap; gap: .7rem; }
    .color-choice { width: 2rem; height: 2rem; border: 3px solid #202842; border-radius: 50%; outline: 1px solid transparent; background: var(--swatch); cursor: pointer; }
    .color-choice.selected { outline-color: #f5b85c; box-shadow: 0 0 0 3px rgba(245,184,92,.13); }
    .selected-color { display: block; margin-top: .7rem; color: #8996b4; font-size: .78rem; }
    .avatar-section { margin-bottom: 1.1rem; }
    .upload-button { display: inline-flex; padding: .7rem .9rem; border: 1px solid rgba(189,204,242,.2); border-radius: .65rem; color: #e4e9fa; background: rgba(255,255,255,.05); font-size: .82rem; font-weight: 700; cursor: pointer; }
    .upload-button:hover, .remove-avatar:hover { border-color: rgba(245,184,92,.55); }
    .visually-hidden { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0,0,0,0); white-space: nowrap; clip-path: inset(50%); }
    .remove-avatar { padding: .7rem; border: 1px solid transparent; color: #aeb8d3; background: transparent; font: inherit; font-size: .8rem; cursor: pointer; }
    .message { padding: .75rem .85rem; border-radius: .6rem; font-size: .85rem; }
    .error { color: #ffb6b6; background: rgba(220,70,80,.12); }
    .success { color: #9fe2c1; background: rgba(89,190,139,.12); }
    .form-footer { display: flex; align-items: center; justify-content: space-between; gap: 1rem; }
    .form-footer > span { color: #7e8aa8; font-size: .75rem; }
    .save-button { display: inline-flex; align-items: center; justify-content: center; gap: .8rem; padding: .8rem 1.1rem; border: 0; border-radius: .7rem; color: #191a20; background: #f5b85c; font: inherit; font-size: .85rem; font-weight: 850; text-decoration: none; cursor: pointer; }
    .save-button:hover { background: #ffca78; }
    .save-button:disabled { opacity: .65; cursor: wait; }
    .empty-card { padding: 2rem; }
    .empty-card h2 { margin-top: 0; }
    .empty-card p { margin: 0 0 1.2rem; color: #aeb8d3; }
    @media (max-width: 700px) { .editor-layout { grid-template-columns: 1fr; } .preview-panel { position: static; } }
    @media (max-width: 460px) { .form-footer { align-items: stretch; flex-direction: column; } .form-footer .save-button { width: 100%; box-sizing: border-box; } }
</style>
