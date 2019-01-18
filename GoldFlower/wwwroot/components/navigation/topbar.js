export default {
	name: 'top-bar',
	props: ['events'],
	template: `
		<!-- Topnav -->
		<nav class="navbar navbar-top navbar-expand navbar-dark bg-primary border-bottom">
		<div class="container-fluid">
			<div class="collapse navbar-collapse" id="navbarSupportedContent">			
				<!-- Navbar links -->
				<ul class="navbar-nav align-items-center ml-md-auto">
					<li class="nav-item dropdown">
						<a class="nav-link" href="#" role="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
							<span class="badge badge-warning">
							<span class="fa fa-bell"></span>
								<strong>{{ events.length }}</strong>
							</span>
						</a>
						<div class="dropdown-menu dropdown-menu-xl dropdown-menu-right py-0 overflow-hidden events">
							<!-- Dropdown header -->
							<div class="px-3 py-3 bg-secondary">
								<h6 class="text-sm m-0">Events</h6>
							</div>

							<!-- List group -->
							<div class="list-group list-group-flush events-list">
								<a class="list-group-item list-group-item-action" v-for="(item, index) in events" :key="index">
									<div class="row align-items-center">
										<div class="col-12">
											<div class="d-flex justify-content-between align-items-center">
												<div class="text-right text-muted">
													<small>{{ new Date(item.dateTime).toLocaleTimeString() }}</small>
												</div>
											</div>
											<p class="text-sm mb-0" :class="convertErrorTypeToClass('text', item.type)" v-html="item.message"></p>
										</div>
									</div>
								</a>
								<a class="list-group-item list-group-item-action" v-if="!events.length">
									<div class="row align-items-center">
										<div class="col-12">
											<p class="text-sm mb-0">No events published yet</p>
										</div>
									</div>
								</a>
							</div>
						</div>
					</li>					
				</ul>			
			</div>
		</div>
	</nav>
	`,
	methods: {
		convertErrorTypeToClass: function(string, type) {
			switch(type) {
				case 0: return `${string}-danger`;
				case 1: return `${string}-warning`;
				case 2: return `${string}-success`;
				case 3: return `${string}-info`;
				case 4: return `${string}-primary`;
				default: return `${string}-light`;
			}
		}
	}
}