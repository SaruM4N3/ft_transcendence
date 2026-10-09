import { json } from '@sveltejs/kit';
import { hash, verify } from '@node-rs/argon2';
import { prisma } from '$lib/server/db.js';
import { createSession } from '$lib/server/session.js';
import type { RequestHandler } from './$types';

const DUMMY_HASH = await hash('dummy-password');

export const POST: RequestHandler = async ({ request, cookies }) => {
    let body: { identifier?: unknown; password?: unknown };
    try {
        body = await request.json();
    } catch {
        return json({ success: false, error: 'Invalid JSON' }, { status: 400 });
    }

    const { identifier, password } = body;
    if (typeof identifier !== 'string' || typeof password !== 'string'
        || !identifier || !password) {
        return json({ success: false, error: 'Missing required fields' }, { status: 400 });
    }

    try {
        const user = await prisma.user.findFirst({
            where: {
                OR: [
                    { email: identifier.trim() },
                    { displayName: identifier.trim() }
                ]
            },
            select: { id: true, email: true, displayName: true, passwordHash: true }
        });

        const valid = await verify(user ? user.passwordHash : DUMMY_HASH, password);

        if (!user || !valid) {
            return json(
                { success: false, error: 'Incorrect email/display name or password' },
                { status: 401 }
            );
        }

        await createSession(user.id, cookies);

        return json({
            success: true,
            user: { id: user.id, email: user.email, display_name: user.displayName }
        });
    } catch (error) {
        console.error('Login failed:', error);
        return json({ success: false, error: 'Login failed' }, { status: 500 });
    }
};
