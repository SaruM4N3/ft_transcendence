<script>
	import { goto } from '$app/navigation';
</script>

<section class="hero">
	<div class="hero-copy">
		<p class="eyebrow"><span></span> YOUR NEXT MATCH STARTS HERE</p>
		<h1>Ready to<br /><span>play?</span></h1>
		<p class="intro">Jump into the game, meet the team, and make this space your own.</p>
		<div class="actions">
			<button class="button" onclick={() => goto('/game')}>Play now <span aria-hidden="true">↗</span></button>
			<button class="button button-outline" onclick={() => goto('/setting')}>Your settings</button>
		</div>
		<div class="quick-links">
			<button onclick={() => goto('/project')}>About the project <span>→</span></button>
			<button onclick={() => goto('/team')}>Meet the team <span>→</span></button>
		</div>
	</div>
	<div class="hero-art" aria-hidden="true">
		<div class="orbit orbit-one"></div>
		<div class="orbit orbit-two"></div>
		<div class="orb"></div>
		<div class="spark spark-one">✦</div>
		<div class="spark spark-two">✧</div>
		<div class="art-label">GAME<br />ON</div>
	</div>
</section>

<style>
	.hero {
		display: grid;
		min-height: min(68vh, 650px);
		grid-template-columns: 1fr 0.9fr;
		align-items: center;
		gap: 3rem;
	}

	.hero-copy {
		max-width: 580px;
	}

	.eyebrow {
		display: flex;
		align-items: center;
		gap: 0.6rem;
		margin: 0 0 1.25rem;
		color: #f5b85c;
		font-size: 0.72rem;
		font-weight: 800;
		letter-spacing: 0.16em;
	}

	.eyebrow span {
		width: 0.45rem;
		height: 0.45rem;
		border-radius: 50%;
		background: #f5b85c;
		box-shadow: 0 0 1rem #f5b85c;
	}

	h1 {
		margin: 0;
		font-size: clamp(4rem, 9vw, 7.5rem);
		line-height: 0.9;
		letter-spacing: -0.075em;
	}

	h1 span {
		color: #f5b85c;
	}

	.intro {
		max-width: 410px;
		margin: 1.6rem 0 1.8rem;
		color: #aeb8d3;
		font-size: 1.08rem;
		line-height: 1.7;
	}

	.actions {
		display: flex;
		flex-wrap: wrap;
		gap: 0.8rem;
	}

	.actions :global(.button) {
		gap: 0.7rem;
	}

	.actions :global(.button-outline) {
		border-color: rgba(189, 204, 242, 0.23);
		color: #edf2ff;
		background: rgba(255, 255, 255, 0.04);
	}

	.actions :global(.button-outline:hover) {
		background: rgba(255, 255, 255, 0.09);
	}

	.quick-links {
		display: flex;
		flex-wrap: wrap;
		gap: 1.5rem;
		margin-top: 2.6rem;
	}

	.quick-links button {
		padding: 0;
		border: 0;
		color: #8995b2;
		background: none;
		font-size: 0.85rem;
		cursor: pointer;
	}

	.quick-links button span {
		margin-left: 0.35rem;
		color: #f5b85c;
	}

	.hero-art {
		position: relative;
		width: min(100%, 440px);
		aspect-ratio: 1;
		justify-self: center;
	}

	.orbit {
		position: absolute;
		inset: 10%;
		border: 1px solid rgba(144, 164, 214, 0.18);
		border-radius: 50%;
		transform: rotate(-32deg) scaleY(0.68);
	}

	.orbit-one {
		inset: 2% -4%;
		border-color: rgba(245, 184, 92, 0.32);
		transform: rotate(38deg) scaleY(0.63);
	}

	.orbit-two {
		transform: rotate(-48deg) scaleY(0.78);
	}

	.orb {
		position: absolute;
		inset: 19%;
		border: 1px solid rgba(255, 255, 255, 0.2);
		border-radius: 50%;
		background: radial-gradient(circle at 32% 25%, #6977a1, #333d65 28%, #1c2444 67%, #11172e);
		box-shadow: inset -18px -24px 45px rgba(0, 0, 0, 0.42), 0 0 90px rgba(81, 109, 181, 0.2);
	}

	.art-label {
		position: absolute;
		right: 13%;
		bottom: 18%;
		color: #f5b85c;
		font-size: 0.8rem;
		font-weight: 900;
		letter-spacing: 0.2em;
		line-height: 1.6;
		transform: rotate(-12deg);
	}

	.spark {
		position: absolute;
		color: #f5b85c;
		font-size: 1.7rem;
	}

	.spark-one { top: 17%; right: 20%; }
	.spark-two { bottom: 25%; left: 11%; color: #9fadd7; }

	@media (max-width: 760px) {
		.hero {
			grid-template-columns: 1fr;
			gap: 0.5rem;
			padding-top: 1rem;
		}

		.hero-art {
			width: min(78vw, 340px);
			grid-row: 1;
		}

		h1 { font-size: clamp(4rem, 18vw, 6rem); }
	}
</style>
