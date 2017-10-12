import Knex from 'knex';
import Bookshelf from 'bookshelf';
import bookshelfUpsert from 'bookshelf-upsert';
import knexfile from './knexfile';

const knex = Knex(knexfile.production);

const bookshelf = Bookshelf(knex);

bookshelf.plugin(bookshelfUpsert);
bookshelf.plugin('pagination');

export default bookshelf;
