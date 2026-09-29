<script>
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';

    let unityCanvas;
    let loadingBar;
    let progressBar;
    let warningBanner;
    let unityInstance = null;
    let errorMessage = '';

    function unityShowBanner(msg, type) {
        if (!warningBanner) return;

        const div = document.createElement('div');
        div.textContent = msg;

        if (type === 'error') {
            div.style = 'background: red; padding: 10px;';
        } else if (type === 'warning') {
            div.style = 'background: yellow; padding: 10px;';
        }

        warningBanner.appendChild(div);

        if (type === 'warning') {
            setTimeout(() => {
                div.remove();
            }, 5000);
        }
    }

    onMount(() => {
        const buildUrl = '/public/game/Build';

        const config = {
            arguments: [],
            dataUrl: `${buildUrl}/game.data.gz`,
            frameworkUrl: `${buildUrl}/game.framework.js.gz`,
            codeUrl: `${buildUrl}/game.wasm.gz`,
            streamingAssetsUrl: '/public/game/StreamingAssets',
            companyName: 'DefaultCompany',
            productName: 'ft_transcendence',
            productVersion: '1.0',
            showBanner: unityShowBanner
        };

        loadingBar.style.display = 'block';

        const script = document.createElement('script');
        script.src = `${buildUrl}/game.loader.js`;

        script.onload = () => {
            createUnityInstance(
                unityCanvas,
                config,
                (progress) => {
                    progressBar.style.width = `${progress * 100}%`;
                }
            )
                .then((instance) => {
                    unityInstance = instance;
                    loadingBar.style.display = 'none';
                })
                .catch((message) => {
                    errorMessage = message;
                    loadingBar.style.display = 'none';
                    unityShowBanner(message, 'error');
                });
        };

        script.onerror = () => {
            errorMessage = 'Failed to load Unity WebGL loader.';
            loadingBar.style.display = 'none';
        };

        document.body.appendChild(script);

        return () => {
            if (unityInstance) {
                unityInstance.Quit();
                unityInstance = null;
            }

            script.remove();
        };
    });
</script>

<div class="game-page">
    <div class="unity-container">
        <canvas
            bind:this={unityCanvas}
            id="unity-canvas"
            tabindex="-1"
        ></canvas>

        <div bind:this={loadingBar} id="unity-loading-bar">
            <div id="unity-progress-bar-empty">
                <div
                    bind:this={progressBar}
                    id="unity-progress-bar-full"
                ></div>
            </div>
        </div>

        <div bind:this={warningBanner} id="unity-warning"></div>
    </div>

    {#if errorMessage}
        <p class="error">{errorMessage}</p>
    {/if}
</div>

<style>
    :global(body) {
        margin: 0;
        cursor: none !important;

    }

    .game-page {
        width: 100vw;
        height: 100vh;
        overflow: hidden;
    }

    .unity-container {
        width: 100%;
        height: 100%;
    }

    #unity-canvas {
        width: 100%;
        height: 100%;
        cursor: none !important;
    }

    #unity-loading-bar {
        display: none;
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        width: 300px;
    }

    #unity-progress-bar-empty {
        width: 100%;
        height: 10px;
        background: #444;
    }

    #unity-progress-bar-full {
        width: 0%;
        height: 100%;
        background: white;
    }

    #unity-warning {
        position: absolute;
        top: 10px;
        left: 10px;
        right: 10px;
    }

    .error {
        color: red;
    }
</style>