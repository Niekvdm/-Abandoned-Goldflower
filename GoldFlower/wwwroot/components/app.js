import TopBar from './navigation/topbar.js';
import ContentHeader from './header.js';
import MainContent from './content.js';

export default {
	name: 'App',
	components: {
		TopBar,
		ContentHeader,
		MainContent
	},
	data() {
		return {
			events: []
		};
	},
	template: `
		<div class="main-content" id="panel">
			<top-bar :events="events"></top-bar>
			<content-header></content-header>
			<main-content></main-content>
		</div>
	`
};
