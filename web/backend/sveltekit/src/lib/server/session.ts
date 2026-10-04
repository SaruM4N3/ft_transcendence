import { pool } from '$lib/server/db.js';
import type { RequestEvent } from '@sveltejs/kit';

export async function POST ({ request}: RequestEvent) {
    try{

    } 
    catch(error){
        console.error('session creation failed:', error);
    }
}