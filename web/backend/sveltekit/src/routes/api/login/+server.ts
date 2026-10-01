import { verify } from '@node-rs/argon2';
import { pool } from '$lib/server/db.js';
import type { RequestEvent } from '@sveltejs/kit';

export async function POST({ request }: RequestEvent) {
    try {
        const { identifier, password } = await request.json();

        if (!identifier || !password) {
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

        const result = await pool.query(
            `SELECT id, email, display_name, password_hash
             FROM users
             WHERE email = $1 OR display_name = $1`,
            [identifier]
        );

        const existingUser = result.rows[0];

        if (!existingUser) {
            return new Response(
                JSON.stringify({
                    success: false,
                    error: 'Incorrect email/display name or password'
                }),
                {
                    status: 401,
                    headers: {
                        'Content-Type': 'application/json'
                    }
                }
            );
        }

        const validPassword = await verify(
            existingUser.password_hash,
            password
        );

        if (!validPassword) {
            return new Response(
                JSON.stringify({
                    success: false,
                    error: 'Incorrect email/display name or password'
                }),
                {
                    status: 401,
                    headers: {
                        'Content-Type': 'application/json'
                    }
                }
            );
        }

        return new Response(
            JSON.stringify({
                success: true,
                user: {
                    id: existingUser.id,
                    email: existingUser.email,
                    display_name: existingUser.display_name
                }
            }),
            {
                status: 200,
                headers: {
                    'Content-Type': 'application/json'
                }
            }
        );
    } catch (error) {
        console.error('Login failed:', error);

        return new Response(
            JSON.stringify({
                success: false,
                error: 'Login failed'
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

