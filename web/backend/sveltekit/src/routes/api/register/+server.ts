import { hash } from '@node-rs/argon2';
import { pool } from '$lib/server/db.js';
import type { RequestEvent } from '@sveltejs/kit';

export async function POST({ request }: RequestEvent) {
    try {
        const { email, password, display_name } = await request.json();

        if (!email || !password || !display_name) {
            return new Response(
                JSON.stringify({
                    success: false,
                    error: 'Missing required fields'
                }),
                {
                    status: 400,
                    headers: {
                        'Content-Type': 'application/json'
                    }
                }
            );
        }

        const password_hash = await hash(password);

        const result = await pool.query(
            `INSERT INTO users (email, password_hash, display_name)
             VALUES ($1, $2, $3)
             RETURNING id, email, display_name, created_at`,
            [email, password_hash, display_name]
        );

        return new Response(
            JSON.stringify({
                success: true,
                user: result.rows[0]
            }),
            {
                status: 201,
                headers: {
                    'Content-Type': 'application/json'
                }
            }
        );
    } catch (error) {
        console.error('Registration failed:', error);

        return new Response(
            JSON.stringify({
                success: false,
                error: 'Registration failed'
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