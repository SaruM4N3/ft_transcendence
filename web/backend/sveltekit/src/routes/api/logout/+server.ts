import type { RequestEvent } from '@sveltejs/kit';
import { pool } from '$lib/server/db.js';

export async function POST({ cookies }: RequestEvent) {
    try {
        const sessionId = cookies.get('session');

        if (sessionId) {
            await pool.query(
                `DELETE FROM sessions WHERE id = $1`,
                [sessionId]
            );
        }

        cookies.delete('session', {
            path: '/'
        });

        return new Response(
            JSON.stringify({
                success: true
            }),
            {
                status: 200,
                headers: {
                    'Content-Type': 'application/json'
                }
            }
        );
    } catch (error) {
        console.error('Logout failed:', error);

        return new Response(
            JSON.stringify({
                success: false,
                error: 'Logout failed'
            }),
            {
                status: 500,
                headers: {
                    'Content-Type': 'application/json'
                }
            }
        );
    }
}
