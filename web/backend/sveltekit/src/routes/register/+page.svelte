<script>
    let email = '';
    let display_name = '';
    let password = '';

    let error = '';
    let success = '';

    async function register() {
        error = '';
        success = '';

        const response = await fetch('/api/register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                email,
                display_name,
                password
            })
        });

        const data = await response.json();

        if (!response.ok) {
            error = data.error;
            return;
        }

        success = 'Account created successfully!';
    }
</script>

<h2>Create an account</h2>

<form on:submit|preventDefault={register}>
    <label for="email">Email:</label><br>
    <input
        type="email"
        id="email"
        bind:value={email}
        autocomplete="off"
        required
    ><br>

    <label for="display_name">Display name:</label><br>
    <input
        type="text"
        id="display_name"
        bind:value={display_name}
        autocomplete="off"
        required
    ><br>

    <label for="password">Password:</label><br>
    <input
        type="password"
        id="password"
        bind:value={password}
        required
    ><br>

    <button type="submit">Register</button>
</form>

{#if error}
    <p>{error}</p>
{/if}

{#if success}
    <p>{success}</p>
{/if}
