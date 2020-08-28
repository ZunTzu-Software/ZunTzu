#pragma once

/* -----------------------------------------------------------------------------
	  Copyright (c) 2020 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

const unsigned int TILE_SLOT_COUNT = 3;

struct tile_slot {
	unsigned int mipmap_level;
	unsigned int x;
	unsigned int y;
	char * texels;
};

typedef int error_code;	// no error if 0, otherwise abort

class synchronized_tile_buffer {
public:
	synchronized_tile_buffer(unsigned int tile_slot_count, size_t buffer_size);
	~synchronized_tile_buffer();

	bool allocate_write_slot(unsigned int & index);	// proceed if true, otherwise abort
	void free_write_slot(unsigned int index);
	void stop_consumer(error_code error);

	error_code allocate_read_slot(unsigned int & index);	// no error if 0, otherwise abort
	void free_read_slot(unsigned int index);
	void stop_producer();

	tile_slot * get_slot(unsigned int index) { return &slots[index]; }

private:
	enum tile_slot_state {
		TILE_SLOT_READY_FOR_WRITING,
		TILE_SLOT_WRITING,
		TILE_SLOT_READY_FOR_READING,
		TILE_SLOT_READING,
	};

	CRITICAL_SECTION critical_section;
	HANDLE full_slots_semaphore;
	HANDLE empty_slots_semaphore;
	volatile bool stop;
	volatile error_code error;
	unsigned int tile_slot_count;
	tile_slot_state * slot_states;	// at least 3 slots
	tile_slot * slots;	// at least 3 slots
};
