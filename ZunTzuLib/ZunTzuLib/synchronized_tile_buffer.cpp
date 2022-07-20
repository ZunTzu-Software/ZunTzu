/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "synchronized_tile_buffer.h"

synchronized_tile_buffer::synchronized_tile_buffer(unsigned int tile_slot_count, size_t buffer_size) :
	full_slots_semaphore(CreateSemaphore(0, 0, tile_slot_count, 0)),
	empty_slots_semaphore(CreateSemaphore(0, tile_slot_count, tile_slot_count, 0)),
	stop(false),
	error(0),
	tile_slot_count(tile_slot_count),
	slot_states(new tile_slot_state[tile_slot_count]),
	slots(new tile_slot[tile_slot_count])
{
	InitializeCriticalSection(&critical_section);
	for(unsigned int i = 0; i < tile_slot_count; ++i) {
		slot_states[i] = TILE_SLOT_READY_FOR_WRITING;
		slots[i].texels = new char[buffer_size];
	}
}

synchronized_tile_buffer::~synchronized_tile_buffer() {
	for(unsigned int i = 0; i < tile_slot_count; ++i) {
		delete [] slots[i].texels;
	}
	DeleteCriticalSection(&critical_section);
	delete [] slots;
	delete [] slot_states;
	CloseHandle(empty_slots_semaphore);
	CloseHandle(full_slots_semaphore);
}

bool synchronized_tile_buffer::allocate_write_slot(unsigned int & index) {
	WaitForSingleObject(empty_slots_semaphore, INFINITE);
	EnterCriticalSection(&critical_section);
	if(!stop) {
		for(unsigned int i = 0; i < tile_slot_count; ++i) {
			if(slot_states[i] == TILE_SLOT_READY_FOR_WRITING) {
				slot_states[i] = TILE_SLOT_WRITING;
				LeaveCriticalSection(&critical_section);
				index = i;
				return true;
			}
		}
	}
	LeaveCriticalSection(&critical_section);
	return false;
}

void synchronized_tile_buffer::free_write_slot(unsigned int index) {
	EnterCriticalSection(&critical_section);
		slot_states[index] = TILE_SLOT_READY_FOR_READING;
	LeaveCriticalSection(&critical_section);
	ReleaseSemaphore(full_slots_semaphore, 1, 0);
}

void synchronized_tile_buffer::stop_consumer(error_code code) {
	EnterCriticalSection(&critical_section);
		error = code;
	LeaveCriticalSection(&critical_section);
	ReleaseSemaphore(full_slots_semaphore, tile_slot_count, 0);
}

error_code synchronized_tile_buffer::allocate_read_slot(unsigned int & index) {
	WaitForSingleObject(full_slots_semaphore, INFINITE);
	EnterCriticalSection(&critical_section);
	error_code err = error;
	if(err == 0) {
		for(unsigned int i = 0; i < tile_slot_count; ++i) {
			if(slot_states[i] == TILE_SLOT_READY_FOR_READING) {
				slot_states[i] = TILE_SLOT_READING;
				LeaveCriticalSection(&critical_section);
				index = i;
				return err;
			}
		}
	}
	LeaveCriticalSection(&critical_section);
	return err;
}

void synchronized_tile_buffer::free_read_slot(unsigned int index) {
	EnterCriticalSection(&critical_section);
		slot_states[index] = TILE_SLOT_READY_FOR_WRITING;
	LeaveCriticalSection(&critical_section);
	ReleaseSemaphore(empty_slots_semaphore, 1, 0);
}

void synchronized_tile_buffer::stop_producer() {
	EnterCriticalSection(&critical_section);
		stop = true;
		error = -1;
	LeaveCriticalSection(&critical_section);
	ReleaseSemaphore(empty_slots_semaphore, tile_slot_count, 0);
	ReleaseSemaphore(full_slots_semaphore, tile_slot_count, 0);
}
